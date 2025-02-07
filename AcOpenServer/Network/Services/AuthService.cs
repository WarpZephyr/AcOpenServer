using AcOpenServer.Logging;
using AcOpenServer.Network.Clients;
using AcOpenServer.Network.Streams;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AcOpenServer.Network.Services
{
    public class AuthService : IService
    {
        private readonly Logger Log;
        private readonly SVFWMessageListener Listener;
        private readonly List<Task> ClientReceiveTasks;
        private readonly List<Task> ClientSendTasks;
        private bool disposedValue;

        public bool IsDisposed => disposedValue;

        public AuthService(SVFWMessageListener listener, Logger log)
        {
            Log = log;
            Listener = listener;
            ClientReceiveTasks = [];
            ClientSendTasks = [];
        }

        #region IO

        public Task ListenAsync()
        {
            Log.Info($"Started {nameof(AuthService)}");
            Listener.Accepted += OnAccepted;
            return Listener.ListenAsync();
        }

        #endregion

        #region Callbacks

        private void OnAccepted(object? sender, SVFWMessageClient client)
        {
            var authClient = new AuthClient(client, Log);
            Log.Info($"Client connected: {authClient.Name}");

            ClientReceiveTasks.Add(authClient.ReceiveAsync().ContinueWith(ClientReceiveCleanup));
            ClientSendTasks.Add(authClient.SendAsync().ContinueWith(ClientSendCleanup));
        }

        #endregion

        #region Cleanup

        private void ClientReceiveCleanup(Task task)
        {
            if (task.Exception != null)
            {
                Log.Error($"Client disconnected due to an error: {task.Exception}");
            }

            ClientReceiveTasks.Remove(task);
        }

        private void ClientSendCleanup(Task task)
        {
            if (task.Exception != null)
            {
                Log.Error($"Client disconnected due to an error: {task.Exception}");
            }

            ClientSendTasks.Remove(task);
        }

        #endregion

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Listener.Dispose();
                    ClientReceiveTasks.Clear();
                    ClientSendTasks.Clear();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            System.GC.SuppressFinalize(this);
        }

        #endregion

        #region IAsyncDisposable

        public async ValueTask DisposeAsync()
        {
            if (!disposedValue)
            {
                Listener.Dispose();
                foreach (var task in ClientReceiveTasks)
                    await task;

                foreach (var task in ClientSendTasks)
                    await task;

                ClientReceiveTasks.Clear();
                ClientSendTasks.Clear();
                disposedValue = true;
            }
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
