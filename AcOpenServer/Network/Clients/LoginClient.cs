using AcOpenServer.Core.Logging;
using AcOpenServer.Network.Exceptions;
using AcOpenServer.Network.Streams;
using SVFWRequestMessage;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AcOpenServer.Network.Clients
{
    public class LoginClient : IDisposable, IAsyncDisposable
    {
        private readonly Logger Log;
        private readonly SVFWMessageClient Client;
        private readonly Queue<Task> SendQueue;
        private readonly int AuthPort;
        private bool disposedValue;

        public string Name => Client.Name;
        public bool IsDisposed => disposedValue;

        public LoginClient(SVFWMessageClient client, int authPort, Logger log)
        {
            Log = log;
            Client = client;
            AuthPort = authPort;
            SendQueue = [];
        }

        private void Service(SVFWMessage message)
        {
            if (message.Header.MessageType != SVFWMessageType.RequestQueryLoginServerInfo)
            {
                throw new LoginException($"Received an unexpected message type from client; Received: {message.Header.MessageType}; Expected: {SVFWMessageType.RequestQueryLoginServerInfo}");
            }

            RequestQueryLoginServerInfo request;
            try
            {
                request = RequestQueryLoginServerInfo.Parser.ParseFrom(message.Payload);
            }
            catch (Exception ex)
            {
                throw new LoginException("Failed to parse message from client as a protobuf.", ex);
            }

            Log.Info($"User {request.PlayerId} is trying to login.");
            if (request.HasF2)
                Log.Debug($"User f2 is {request.F2}");
            Log.Debug($"User app version is 0x{request.AppVersion:X2}");
            var response = new RequestQueryLoginServerInfoResponse
            {
                Port = (uint)AuthPort,
            };

            SendQueue.Enqueue(Client.SendAsync(response, SVFWMessageType.Reply, message.Header.MessageIndex));
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
