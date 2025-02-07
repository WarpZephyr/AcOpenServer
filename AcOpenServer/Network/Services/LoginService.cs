using AcOpenServer.Logging;
using AcOpenServer.Network.Clients;
using AcOpenServer.Network.Streams;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AcOpenServer.Network.Services
{
    public class LoginService : IService
    {
        private readonly Logger Log;
        private readonly SVFWMessageListener Listener;
        private readonly List<Task> ClientReceiveTasks;
        private readonly List<Task> ClientSendTasks;
        private readonly int AuthPort;
        private bool disposedValue;

        public bool IsDisposed => disposedValue;

        public LoginService(SVFWMessageListener listener, int authPort, Logger log)
        {
            Log = log;
            Listener = listener;
            ClientReceiveTasks = [];
            ClientSendTasks = [];
            AuthPort = authPort;
        }

        #region IO

        public Task ListenAsync()
        {
            Log.Info($"Started {nameof(LoginService)}");
            Listener.Accepted += OnAccepted;
            return Listener.ListenAsync();
        }

        #endregion

        #region Callbacks

        private void OnAccepted(object? sender, SVFWMessageClient client)
        {
            var loginClient = new LoginClient(client, AuthPort, Log);
            Log.Info($"{nameof(LoginService)}: Client connected: {loginClient.Name}");

            ClientReceiveTasks.Add(loginClient.ReceiveAsync().ContinueWith(ClientReceiveCleanup));
            ClientSendTasks.Add(loginClient.SendAsync().ContinueWith(ClientSendCleanup));
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
