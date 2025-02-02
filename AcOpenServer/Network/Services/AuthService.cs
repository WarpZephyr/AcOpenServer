using AcOpenServer.Core.Crypto;
using AcOpenServer.Core.Logging;
using AcOpenServer.Core.Network;
using AcOpenServer.Network.Clients;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace AcOpenServer.Network.Services
{
    public class AuthService : IService
    {
        private readonly Logger Log;
        private readonly string ServerName;
        private readonly NetConnection Connection;
        private readonly RSAKey ServerKey;
        private readonly int AuthPort;
        private readonly double ClientTimeout;
        private bool disposedValue;

        private readonly List<AuthClient> Clients;

        public AuthService(string serverName, IPAddress serverIP, RSAKey serverKey, int authPort, double clientTimeout, Logger log)
        {
            Log = log;
            ServerName = serverName;
            Connection = new NetConnection(nameof(AuthService), serverIP, authPort);
            Connection.Accepted += OnAccepted;
            ServerKey = serverKey;
            AuthPort = authPort;
            ClientTimeout = clientTimeout;

            Clients = [];
        }

        public bool Start()
        {
            Connection.Listen();
            Log.Info($"Started {ServerName} server service {nameof(AuthService)} on port {AuthPort}");
            return true;
        }

        public bool End()
        {
            Log.Info($"Ending {ServerName} server service {nameof(AuthService)} on port {AuthPort}");
            Dispose();
            return true;
        }

        public async Task UpdateAsync()
        {
            await UpdateClientsAsync();
        }

        private async Task UpdateClientsAsync()
        {
            for (int i = Clients.Count - 1; i >= 0; i--)
            {
                var client = Clients[i];
                if (!await client.UpdateAsync())
                {
                    client.Dispose();
                    Clients.RemoveAt(i);
                }
            }
        }

        private void OnAccepted(object? sender, NetConnectionEventArgs e)
        {
            var connection = e.Connection;
            if (connection != null)
            {
                var client = new AuthClient($"{ServerName}:{connection.GetName()}", connection, ServerKey, AuthPort, ClientTimeout, Log);
                Log.Info($"Client connected: {client.Name}");
                Clients.Add(client);
            }
        }

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Connection.Dispose();

                    foreach (var client in Clients)
                    {
                        client.Dispose();
                    }

                    Clients.Clear();
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
    }
}
