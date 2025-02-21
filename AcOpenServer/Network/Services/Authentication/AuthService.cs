using AcOpenServer.Logging;
using AcOpenServer.Network.Communication.SVFW;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AcOpenServer.Network.Services.Authentication
{
    public class AuthService : IService
    {
        private readonly SVFWMessageListener Listener;
        private readonly AuthConfig Config;
        private readonly ScopeLog Log;
        private readonly PeriodicTimer PollTimer;
        private bool disposedValue;

        public bool IsDisposed => disposedValue;

        public AuthService(SVFWMessageListener listener, AuthConfig config, ScopeLog log)
        {
            Listener = listener;
            Config = config;
            Log = log;
            PollTimer = new PeriodicTimer(TimeSpan.FromSeconds(2));
        }

        #region IO

        public Task ListenAsync()
        {
            Log.Info("Started");
            Listener.Accepted += OnAccepted;
            return Listener.ListenAsync();
        }

        #endregion

        #region Network

        private async Task PollClient(AuthClient client)
        {
            while (await PollTimer.WaitForNextTickAsync())
            {
                if (!client.IsConnected())
                {
                    Log.Info($"Client disconnected: {client.Name}");
                    return;
                }
            }
        }

        #endregion

        #region Callbacks

        private void OnAccepted(object? sender, SVFWMessageClient messageClient)
        {
            var client = new AuthClient(messageClient, Config, Log.Push(nameof(AuthClient)));
            Log.Info($"Client connected: {client.Name}");

            _ = client.ReceiveAsync().ContinueWith((Task task) => ClientCleanup(task, "receive", client.Name));
            _ = client.SendAsync().ContinueWith((Task task) => ClientCleanup(task, "send", client.Name));
            _ = PollClient(client).ContinueWith((Task task) => ClientCleanup(task, "poll", client.Name));
        }

        #endregion

        #region Cleanup

        private void ClientCleanup(Task task, string type, string clientName)
        {
            if (task.Exception != null)
            {
                Log.Error($"Client {clientName} {type} task had an error: {task.Exception}");
            }
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
    }
}
