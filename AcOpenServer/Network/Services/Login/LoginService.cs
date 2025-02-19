using AcOpenServer.Logging;
using AcOpenServer.Network.Communication;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace AcOpenServer.Network.Services.Login
{
    public class LoginService : IService
    {
        private readonly SVFWMessageListener Listener;
        private readonly LoginConfig Config;
        private readonly ScopeLog Log;
        private readonly PeriodicTimer PollTimer;
        private bool disposedValue;

        public bool IsDisposed => disposedValue;

        public LoginService(SVFWMessageListener listener, LoginConfig config, ScopeLog log)
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

        private async Task PollClient(LoginClient client)
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
            var client = new LoginClient(messageClient, Config, Log.Push(nameof(LoginClient)));
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
                Log.Error($"{nameof(LoginClient)} {clientName} {type} task had an error: {task.Exception}");
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
