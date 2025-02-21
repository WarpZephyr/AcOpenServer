using AcOpenServer.Exceptions;
using AcOpenServer.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace AcOpenServer.Network.Communication.Udp
{
    public class UdpChannelListener : IDisposable
    {
        private readonly ScopeLog Log;
        private readonly UdpClient Listener;
        private readonly Dictionary<IPEndPoint, UdpChannelClient> Clients;
        private readonly CancellationTokenSource CancellationTokenSource;
        private readonly CancellationToken CancellationToken;
        private bool disposedValue;

        public bool IsDisposed => disposedValue;
        public event EventHandler<UdpChannelAcceptedEventArgs>? Accepted;

        public UdpChannelListener(int port, ScopeLog log)
        {
            Log = log;
            Listener = new UdpClient(port, AddressFamily.InterNetwork)
            {
                EnableBroadcast = true
            };

            Clients = [];
            CancellationTokenSource = new CancellationTokenSource();
            CancellationToken = CancellationTokenSource.Token;
        }

        #region IO

        public void Cancel()
            => CancellationTokenSource.Cancel();

        public async Task ListenAsync()
        {
            try
            {
                while (true)
                {
                    var result = await Listener.ReceiveAsync(CancellationToken);
                    var endPoint = result.RemoteEndPoint;
                    var buffer = result.Buffer;

                    if (Clients.TryGetValue(endPoint, out UdpChannelClient? client))
                    {
                        // If we already have the client, update it
                        client.NotifyReceived(buffer);
                    }
                    else if (Accepted != null) // Don't accept client if there is nobody to use it
                    {
                        // If we do not have the client, accept it
                        string name = endPoint.ToString();
                        var newClient = new UdpChannelClient(this, endPoint, name, Log.Push($"{nameof(UdpChannelClient)}->{name}"));
                        Clients.Add(endPoint, newClient);

                        var eventArgs = new UdpChannelAcceptedEventArgs(newClient, buffer);
                        Accepted.Invoke(this, eventArgs);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Log.Info("Listen cancelled.");
            }
        }

        public async Task SendAsync(UdpChannelClient client, byte[] buffer)
        {
            try
            {
                int sent = await Listener.SendAsync(buffer, client.EndPoint, CancellationToken);
                if (sent < buffer.Length)
                {
                    throw new UdpSendException($"Failed to send full buffer for UDP; Sent: {sent}; Expected: {buffer.Length}");
                }
            }
            catch (OperationCanceledException)
            {
                Log.Info("Send cancelled.");
            }
        }

        #endregion

        #region Client

        public bool NotifyClientDisconnect(UdpChannelClient client)
            => Clients.Remove(client.EndPoint);

        #endregion

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Cancel();
                    Accepted = null;
                    foreach (var pair in Clients)
                    {
                        pair.Value.Dispose();
                    }

                    Clients.Clear();
                    CancellationTokenSource.Dispose();
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
