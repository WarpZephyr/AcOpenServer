using AcOpenServer.Logging;
using AcOpenServer.Network.Data.AC;
using AcOpenServer.Network.Communication;
using AcOpenServer.Utilities;
using SVFWRequestMessage;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using AcOpenServer.Network.Data.SVFW;

namespace AcOpenServer.Network.Services.Login
{
    public class LoginClient : IDisposable, IAsyncDisposable
    {
        private readonly SVFWMessageClient Client;
        private readonly LoginConfig Config;
        private readonly ScopeLog Log;
        private readonly Queue<Task> SendQueue;
        private bool disposedValue;

        public string Name => Client.Name;
        public bool IsDisposed => disposedValue;
        public bool Disconnected => disposedValue;

        public LoginClient(SVFWMessageClient client, LoginConfig config, ScopeLog log)
        {
            Client = client;
            Config = config;
            Log = log;
            SendQueue = [];
        }

        private void Service(SVFWMessage message)
        {
            // Validate we are getting the expected message
            if (message.Header.MessageType != SVFWMessageType.RequestQueryLoginServerInfo)
            {
                Disconnect($"Disconnecting client {Name} due to an unexpected message type for {nameof(RequestQueryLoginServerInfo)}: {message.Header.MessageType}");
                return;
            }

            // Parse the message protobuf
            if (!ProtobufHelper.TryParse(message.Payload, out RequestQueryLoginServerInfo? request, out string? error))
            {
                Disconnect($"Disconnecting client {Name} due to a {nameof(RequestQueryLoginServerInfo)} parsing failure: {error}");
                return;
            }

            // Build the response
            var appVersion = new AcvAppVersion(request.AppVersion);
            Log.Info($"User logging in: {request.PlayerName} {appVersion}");
            var response = new RequestQueryLoginServerInfoResponse
            {
                Port = Config.AuthPort
            };

            // Send the response
            SendQueue.Enqueue(Client.SendAsync(response, SVFWMessageType.Reply, message.Header.MessageIndex));
            Log.Info($"User logged in: {request.PlayerName}");
        }

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

        public void Disconnect(string message)
        {
            Log.Warn(message);
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
