using AcOpenServer.Crypto;
using AcOpenServer.Logging;
using AcOpenServer.Network.Data.AC;
using AcOpenServer.Network.Data.RPCN;
using AcOpenServer.Network.Exceptions;
using AcOpenServer.Network.Streams;
using AcOpenServer.Utilities;
using Google.Protobuf;
using SVFWRequestMessage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AcOpenServer.Network.Clients
{
    public class AuthClient : IDisposable, IAsyncDisposable
    {
        private readonly Logger Log;
        private readonly SVFWMessageClient Client;
        private readonly Queue<Task> SendQueue;
        private AuthClientState AuthState;
        private string UserName;
        private byte[] GameCwcKeyBytes;
        private CWCKey? GameCwcKey;
        private bool disposedValue;

        public string Name => Client.Name;
        public bool IsDisposed => disposedValue;

        public AuthClient(SVFWMessageClient client, Logger log)
        {
            Log = log;
            Client = client;
            AuthState = AuthClientState.WaitingForHandshakeRequest;
            SendQueue = [];
            UserName = string.Empty;
            GameCwcKeyBytes = new byte[16];
        }

        private void Service(SVFWMessage message)
        {
            switch (AuthState)
            {
                case AuthClientState.WaitingForHandshakeRequest:
                    ValidateState<RequestHandshake>(message.Header.MessageType, SVFWMessageType.RequestHandshake);
                    var handshake = Parse<RequestHandshake>(message);

                    var aesCwcKey = handshake.AesCwcKey.Memory.ToArray();
                    var cwcCipher = new CWCCipher(new CWCKey(aesCwcKey));
                    Client.SetCipher(cwcCipher, cwcCipher);

                    Client.CipherEnabled = false;
                    byte[] responseBuffer = new byte[27];
                    var rand = new Random();
                    rand.NextBytes(responseBuffer);
                    int remaining = 11 + 16;
                    for (int i = 11; i < remaining; i++)
                        responseBuffer[i] = 0;

                    var handshakeResponseMessage = new SVFWMessage(responseBuffer);
                    SendQueue.Enqueue(Client.SendAsync(handshakeResponseMessage, SVFWMessageType.Reply, message.Header.MessageIndex));
                    Client.CipherEnabled = true;
                    AuthState = AuthClientState.WaitingForServiceStatusRequest;
                    break;
                case AuthClientState.WaitingForServiceStatusRequest:
                    ValidateState<GetServiceStatus>(message.Header.MessageType, SVFWMessageType.GetServiceStatus);
                    var serviceStatus = Parse<GetServiceStatus>(message);
                    UserName = serviceStatus.PlayerId;

                    Log.Info($"User {UserName} is trying to authenticate.");
                    var serviceStatusResponse = new GetServiceStatusResponse
                    {
                        Id = 2,
                        PlayerId = "",
                        Unk3 = false,
                        AppVersion = (int)serviceStatus.AppVersion
                    };

                    SendQueue.Enqueue(Client.SendAsync(serviceStatusResponse, SVFWMessageType.Reply, message.Header.MessageIndex));
                    AuthState = AuthClientState.WaitingForKeyMaterial;
                    break;
                case AuthClientState.WaitingForKeyMaterial:
                    ValidateState(message.Header.MessageType, SVFWMessageType.KeyMaterial);
                    byte[] cwcKeyBytes = new byte[16];
                    rand = new Random();
                    rand.NextBytes(cwcKeyBytes);

                    // Client sends 16 bytes
                    // First 8 are app_version again
                    // Second 8 is the key part we need
                    // Client fills first 8 bytes of our key
                    Array.Copy(message.Payload, 8, cwcKeyBytes, 0, 8);

                    // Store our key so we can validate it later
                    // The native libraries encrypt the old buffer so copy it before we send it
                    Array.Copy(cwcKeyBytes, GameCwcKeyBytes, cwcKeyBytes.Length);

                    GameCwcKey = new CWCKey(cwcKeyBytes);
                    var responseKeyMessage = new SVFWMessage(cwcKeyBytes);
                    SendQueue.Enqueue(Client.SendAsync(responseKeyMessage, SVFWMessageType.Reply, message.Header.MessageIndex));

                    AuthState = AuthClientState.WaitingForTicket;
                    break;
                case AuthClientState.WaitingForTicket:
                    var ticket = new Ticket(message.Payload);

                    // Validate the ticket hasn't expired
                    if (ticket.IsExpired)
                        throw new AuthException($"User {UserName} sent a ticket that expired on: {ticket.ExpireDate}");

                    // Warn when ticket isn't signed
                    if (!ticket.IsSigned)
                        Log.Warning($"User {UserName} sent a ticket that isn't signed.");

                    // Validate we are both using the same key
                    var ticketCwcKey = ticket.Cookie[..16];
                    if (!GameCwcKeyBytes.SequenceEqual(ticketCwcKey))
                        throw new AuthException($"User {UserName} returned AES-CWC-128 key that does not match expected key.");

                    // Clear the byte array for security
                    for (int i = 0; i < GameCwcKeyBytes.Length; i++)
                        GameCwcKeyBytes[i] = 0;

                    Log.Info($"User {UserName} authenticated successfully.");
                    AuthState = AuthClientState.Complete;
                    break;
                case AuthClientState.Complete:
                    throw new AuthException($"User {UserName} sent more data to the auth server, but has already completed authentication.");
                default:
                    throw new AuthException($"Unknown authentication step: {AuthState}");
            }
        }

        #region Helpers

        private void ValidateState(SVFWMessageType type, SVFWMessageType expectedType)
        {
            if (type != expectedType)
            {
                throw new AuthException($"Disconnecting client {Name} due to an invalid message: {type}");
            }
        }

        private void ValidateState<T>(SVFWMessageType type, SVFWMessageType expectedType)
        {
            if (type != expectedType)
            {
                throw new AuthException($"Disconnecting client {Name} due to an invalid message for {typeof(T).Name}: {type}");
            }
        }

        private T Parse<T>(SVFWMessage message) where T : IMessage, new()
        {
            try
            {
                T result = ProtobufHelper.ParseFrom<T>(message.Payload);
                return result;
            }
            catch (Exception ex)
            {
                throw new AuthException($"Disconnecting client {Name} due to a {typeof(T).Name} parsing failure.", ex);
            }
        }

        #endregion

        #region IO

        public Task ReceiveAsync()
        {
            Client.Received += OnReceived;
            return Client.ReceiveAsync();
        }

        public async Task SendAsync()
        {
            while (SendQueue.TryDequeue(out Task? sendTask))
            {
                await sendTask;
            }
        }

        #endregion

        #region Callbacks

        private void OnReceived(object? sender, SVFWMessage message)
        {
            Service(message);
        }

        #endregion

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Client.Dispose();
                    SendQueue.Clear();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region IAsyncDisposable

        public async ValueTask DisposeAsync()
        {
            if (!disposedValue)
            {
                Client.Dispose();
                await SendAsync();
                SendQueue.Clear();

                disposedValue = true;
            }
            GC.SuppressFinalize(this);
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            return Name;
        }

        #endregion
    }
}
