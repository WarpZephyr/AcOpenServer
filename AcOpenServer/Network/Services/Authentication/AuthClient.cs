using AcOpenServer.Binary;
using AcOpenServer.Crypto;
using AcOpenServer.Logging;
using AcOpenServer.Network.Communication.SVFW;
using AcOpenServer.Network.Data.AC;
using AcOpenServer.Network.Data.RPCN;
using AcOpenServer.Network.Data.SVFW;
using AcOpenServer.Utilities;
using Google.Protobuf;
using SVFWRequestMessage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace AcOpenServer.Network.Services.Authentication
{
    public class AuthClient : IDisposable, IAsyncDisposable
    {
        private readonly SVFWMessageClient Client;
        private readonly AuthConfig Config;
        private readonly ScopeLog Log;
        private readonly Queue<Task> SendQueue;
        private readonly byte[] GameCwcKeyBytes;

        private AuthClientState AuthState;
        private string PlayerName;
        private CWCKey? GameCwcKey;
        private bool disposedValue;

        public string Name => Client.Name;
        public bool IsDisposed => disposedValue;
        public bool Disconnected => disposedValue;

        public bool Completed => AuthState == AuthClientState.Complete;
        public CWCKey? GameKey => GameCwcKey;

        public AuthClient(SVFWMessageClient client, AuthConfig config, ScopeLog log)
        {
            Client = client;
            Config = config;
            Log = log;
            SendQueue = [];
            GameCwcKeyBytes = new byte[16];

            AuthState = AuthClientState.WaitingForHandshakeRequest;
            PlayerName = string.Empty;
        }

        private void Service(SVFWMessage message)
        {
            switch (AuthState)
            {
                case AuthClientState.WaitingForHandshakeRequest:
                    // Process a handshake request that contains the AES-CWC-128 key used during authentication.
                    // This key is only used for authentication.
                    Handle_WaitingForHandshakeRequest(message);
                    break;
                case AuthClientState.WaitingForServiceStatusRequest:
                    // Process a service status request to confirm whether this client can be serviced.
                    // The response will be empty if the user cannot be serviced.
                    Handle_WaitingForServiceStatusRequest(message);
                    break;
                case AuthClientState.WaitingForKeyMaterial:
                    // Process a key material request that contains part of an AES-CWC-128 key that will be used for everything else.
                    Handle_WaitingForKeyMaterial(message);
                    break;
                case AuthClientState.WaitingForTicket:
                    // Process a ticket sent from the network the game is running on.
                    // The response to this is info about the game server.
                    Handle_WaitingForTicket(message);
                    break;
                case AuthClientState.Complete:
                    Disconnect($"User {PlayerName} sent more data to the auth server, but has already completed authentication.");
                    AuthState = AuthClientState.Disconnected;
                    break;
                case AuthClientState.Disconnected:
                    Debug.Assert(false, $"{nameof(AuthClient)} should not be servicing a disconnected client.");
                    return;
                default:
                    var authState = AuthState;
                    AuthState = AuthClientState.Disconnected;
                    throw new Exception($"Unknown authentication step: {authState}");
            }

        }

        #region States

        private void Handle_WaitingForHandshakeRequest(SVFWMessage message)
        {
            // Validate we are getting the expected message
            if (message.Header.MessageType != SVFWMessageType.RequestHandshake)
            {
                Disconnect($"Disconnecting client {Name} due to an unexpected message type for {nameof(RequestHandshake)}: {message.Header.MessageType}");
                return;
            }

            // Parse the message protobuf
            if (!ProtobufHelper.TryParse(message.Payload, out RequestHandshake? handshake, out string? error))
            {
                Disconnect($"Disconnecting client {Name} due to a {nameof(RequestHandshake)} parsing failure: {error}");
                return;
            }

            // Build the AES-CWC-128 cipher used for authentication
            // This key is only used in authentication steps
            var cwcCipher = new CWCCipher(new CWCKey(handshake.AesCwcKey.Memory.ToArray()));
            Client.SetCipher(cwcCipher, cwcCipher);

            // Build the server response to this, seemingly goes unused
            Client.CipherEnabled = false;
            byte[] responseBuffer = new byte[27];
            RandomHelper.NextBytes(responseBuffer);
            int remaining = 11 + 16;
            for (int i = 11; i < remaining; i++)
                responseBuffer[i] = 0;

            // Send response
            var handshakeResponseMessage = new SVFWMessage(responseBuffer);
            SendQueue.Enqueue(Client.SendAsync(handshakeResponseMessage, SVFWMessageType.Reply, message.Header.MessageIndex));
            Client.CipherEnabled = true;
            AuthState = AuthClientState.WaitingForServiceStatusRequest;
        }

        private void Handle_WaitingForServiceStatusRequest(SVFWMessage message)
        {
            // Validate we are getting the expected message
            if (message.Header.MessageType != SVFWMessageType.GetServiceStatus)
            {
                Disconnect($"Disconnecting client {Name} due to an unexpected message type for {nameof(GetServiceStatus)}: {message.Header.MessageType}");
                return;
            }

            // Parse the message protobuf
            if (!ProtobufHelper.TryParse(message.Payload, out GetServiceStatus? serviceStatus, out string? error))
            {
                Disconnect($"Disconnecting client {Name} due to a {nameof(RequestHandshake)} parsing failure: {error}");
                return;
            }

            // Get user info
            PlayerName = serviceStatus.PlayerName;
            var userAppVersion = new AcvAppVersion(serviceStatus.AppVersion);
            Log.Info($"User authenticating: {PlayerName} {userAppVersion}");

            // Send a response based on the version
            GetServiceStatusResponse serviceStatusResponse;
            bool belowMinimumVersion = userAppVersion < Config.MinimumVersion;
            bool aboveMaximumVersion = userAppVersion > Config.MaximumVersion;
            if (belowMinimumVersion || aboveMaximumVersion)
            {
                // Send an empty response if the version is not valid
                serviceStatusResponse = new GetServiceStatusResponse
                {
                    Id = 0,
                    PlayerName = "",
                    Unk3 = false,
                    AppVersion = 0
                };

                // Selectively show minimum or maximum to not clog the log
                string errorTypeStr = belowMinimumVersion ? $"Minimum: {Config.MinimumVersion};" : $"Maximum: {Config.MaximumVersion};";
                _ = SendThenDisconnect(serviceStatusResponse, message.Header.MessageIndex,
                    $"Disconnecting user {PlayerName} due to receiving an unexpected or unsupported version;"
                    + errorTypeStr
                    + $"Received: {userAppVersion}");
                return;
            }
            else
            {
                // Send a response that confirms the version is valid
                serviceStatusResponse = new GetServiceStatusResponse
                {
                    Id = 2,
                    PlayerName = "",
                    Unk3 = false,
                    AppVersion = (int)serviceStatus.AppVersion
                };

                SendQueue.Enqueue(Client.SendAsync(serviceStatusResponse, SVFWMessageType.Reply, message.Header.MessageIndex));
                AuthState = AuthClientState.WaitingForKeyMaterial;
            }            
        }

        private void Handle_WaitingForKeyMaterial(SVFWMessage message)
        {
            // Validate we are getting the expected message
            if (message.Header.MessageType != SVFWMessageType.KeyMaterial)
            {
                Disconnect($"Disconnecting user {PlayerName} due to an unexpected message type: {message.Header.MessageType}");
                return;
            }

            // Get random bytes for key
            byte[] cwcKeyBytes = new byte[16];
            RandomHelper.NextBytes(cwcKeyBytes);

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
        }

        private void Handle_WaitingForTicket(SVFWMessage message)
        {
            // Parse the ticket
            if (!Ticket.TryParse(message.Payload, out Ticket? ticket, out string? error))
            {
                Disconnect($"Disconnecting {PlayerName} due to a ticket parsing error: {error}");
                return;
            }

            // Validate the ticket hasn't expired
            if (ticket.IsExpired)
            {
                Disconnect($"User sent a ticket that expired on {ticket.ExpireDate}: {PlayerName}");
                return;
            }

            // Warn when ticket isn't signed
            if (!ticket.IsSigned)
                Log.Warn($"User sent a ticket that isn't signed: {PlayerName}");

            // Validate we are both using the same key
            var ticketCwcBytes = ticket.Cookie[..16];
            if (!GameCwcKeyBytes.SequenceEqual(ticketCwcBytes))
            {
                Disconnect($"User returned AES-CWC-128 key that does not match expected key: {PlayerName}");
                return;
            }

            // Clear the byte array for security
            for (int i = 0; i < GameCwcKeyBytes.Length; i++)
                GameCwcKeyBytes[i] = 0;

            // Check if we should send the public or private IP address
            uint gameServerIP;
            if (Client.IsPrivateNetwork())
            {
                Log.Info($"User on private network: {PlayerName}");
                gameServerIP = Config.PrivateIP;
            }
            else
            {
                gameServerIP = Config.PublicIP;
            }

            // Build message
            var serverInfo = new ConnectGameServerPortIdResponse
            {
                AuthToken = 0,
                Address = gameServerIP,
                GamePort = Config.GamePort,
                SendBufferSize = 0x8000,
                ReceiveBufferSize = 0x8000,
                Unk18 = 0xA000,
                Unk1C = 0xA000,
                Unk20 = 0x80,
                Unk24 = 0x8000,
                Unk28 = 0xA000,
                Unk2C = 0x493E0,
                Unk30 = 0x61A8,
                Unk34 = 0xC
            };

            serverInfo.SwapEndian();
            var serverInfoBytes = new byte[56];
            BinaryBufferWriter.Write(serverInfoBytes, 0, serverInfo);
            var serverInfoMessage = new SVFWMessage(serverInfoBytes);

            // Send message
            SendQueue.Enqueue(Client.SendAsync(serverInfoMessage, SVFWMessageType.Reply, message.Header.MessageIndex));
            Log.Info($"User authenticated: {PlayerName}");
            AuthState = AuthClientState.Complete;
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

        #region Network

        public bool IsConnected()
            => Client.IsConnected();

        private async Task SendThenDisconnect(IMessage message, uint messageIndex, string reason)
        {
            Log.Warn(reason);
            AuthState = AuthClientState.Disconnected;
            await Client.SendAsync(message, SVFWMessageType.Reply, messageIndex);
            Disconnect();
        }

        private async Task SendThenDisconnect(SVFWMessage message, uint messageIndex, string reason)
        {
            Log.Warn(reason);
            AuthState = AuthClientState.Disconnected;
            await Client.SendAsync(message, SVFWMessageType.Reply, messageIndex);
            Disconnect();
        }

        private void Disconnect(string message)
        {
            Log.Warn(message);
            AuthState = AuthClientState.Disconnected;
            Disconnect();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Disconnect()
            => Dispose();

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
