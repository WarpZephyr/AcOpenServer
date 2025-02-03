using AcOpenServer.Core.Crypto;
using AcOpenServer.Core.Logging;
using AcOpenServer.Core.Utilities;
using AcOpenServer.Network.Exceptions;
using AcOpenServer.Network.Streams;
using Google.Protobuf;
using SVFWRequestMessage;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AcOpenServer.Network.Clients
{
    public class AuthClient : IDisposable, IAsyncDisposable
    {
        private readonly Logger Log;
        private readonly SVFWMessageClient Client;
        private readonly Queue<Task> SendQueue;
        private AuthClientState AuthState;
        private bool disposedValue;

        public string Name => Client.Name;
        public bool IsDisposed => disposedValue;

        public AuthClient(SVFWMessageClient client, Logger log)
        {
            Log = log;
            Client = client;
            AuthState = AuthClientState.WaitingForHandshakeRequest;
            SendQueue = [];
        }

        private void Service(SVFWMessage message)
        {
            switch (AuthState)
            {
                case AuthClientState.WaitingForHandshakeRequest:
                    ValidateState<RequestHandshake>(message.Header.MessageType, SVFWMessageType.RequestHandshake);
                    var handshake = Parse<RequestHandshake>(message);

                    var cwcCipher = new CWCCipher(new CWCKey(handshake.AesCwcKey.Memory.ToArray()));
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

                    Log.Debug($"Sent {nameof(RequestHandshakeResponse)} to client {Name}");
                    Client.CipherEnabled = true;
                    AuthState = AuthClientState.WaitingForServiceStatusRequest;
                    break;
                case AuthClientState.WaitingForServiceStatusRequest:
                    ValidateState<GetServiceStatus>(message.Header.MessageType, SVFWMessageType.GetServiceStatus);
                    var serviceStatus = Parse<GetServiceStatus>(message);

                    Log.Info($"User {serviceStatus.PlayerId} is trying to authenticate.");

                    var serviceStatusResponse = new GetServiceStatusResponse();
                    //serviceStatusResponse.Id = serviceStatus.Id;
                    serviceStatusResponse.PlayerId = serviceStatus.PlayerId;
                    //serviceStatusResponse.Unknown1 = 0;
                    //serviceStatusResponse.AppVersion = serviceStatus.AppVersion;

                    SendQueue.Enqueue(Client.SendAsync(serviceStatusResponse, SVFWMessageType.Reply, message.Header.MessageIndex));

                    Log.Debug($"Sent {nameof(GetServiceStatusResponse)} to client {Name}");
                    AuthState = AuthClientState.Complete;
                    break;
                case AuthClientState.Complete:
                    Log.Debug(message.Header.MessageType.ToString());
                    Log.Debug(message.Payload.ToHexView(0x10));
                    break;
            }
        }

        #region Helpers

        private void ValidateState<T>(SVFWMessageType type, SVFWMessageType expectedType)
        {
            if (type != expectedType)
            {
                throw new AuthException($"Disconnecting client {Name} due to an invalid message for {typeof(T).Name}: {type}");
            }
        }

        private T Parse<T>(SVFWMessage message) where T : IMessage, new()
        {
            string typeName = typeof(T).Name;
            Log.Info($"Client {Name} has sent {typeName}.");

            try
            {
                T result = ProtobufHelper.ParseFrom<T>(message.Payload);
                return result;
            }
            catch (Exception ex)
            {
                throw new AuthException($"Disconnecting client {Name} due to a {typeName} parsing failure.", ex);
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
