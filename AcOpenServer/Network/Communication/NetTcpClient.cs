using AcOpenServer.Logging;
using AcOpenServer.Utilities;
using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace AcOpenServer.Network.Communication
{
    public class NetTcpClient : IDisposable
    {
        private readonly TcpClient Client;
        private readonly double Timeout;
        private readonly ScopeLog Log;
        private readonly NetworkStream Stream;
        private readonly bool IsPrivateClient;
        private readonly string Name;
        private bool disposedValue;

        public byte[]? Buffer { get; set; }
        public bool IsDisposed => disposedValue;
        public bool Disconnected => disposedValue;

        public event EventHandler<int>? Received;

        public NetTcpClient(TcpClient client, double timeout, ScopeLog log)
        {
            Client = client;
            Timeout = timeout;
            Log = log;
            Stream = client.GetStream();
            IsPrivateClient = IPAddressHelper.IsPrivateRemoteTcpClient(client);

            EndPoint remoteEndPoint = client.Client.RemoteEndPoint ?? throw new Exception("Remote end point was null on a remote connection.");
            Name = $"{remoteEndPoint}";
        }

        #region IO

        public async Task ReceiveAsync()
        {
            if (Timeout > 0d)
            {
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(Timeout));
                    while (true)
                    {
                        if (!Client.Connected)
                        {
                            Log.Warn($"Client {Name} has disconnected.");
                            Client.Close();
                            break;
                        }

                        if (Buffer != null)
                        {
                            int received = await Stream.ReadAsync(Buffer, cts.Token);
                            if (received > 0)
                            {
                                Received?.Invoke(this, received);
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    Log.Warn($"Client {Name} has timed out.");
                    Client.Close();
                }
            }
            else
            {
                while (true)
                {
                    if (!Client.Connected)
                    {
                        Log.Warn($"Client {Name} has disconnected.");
                        Client.Close();
                        break;
                    }

                    if (Buffer != null)
                    {
                        int received = await Stream.ReadAsync(Buffer);
                        if (received > 0)
                        {
                            Received?.Invoke(this, received);
                        }
                    }
                }
            }
        }

        public Task SendAsync(byte[] buffer)
        {
            return Stream.WriteAsync(buffer).AsTask();
        }

        #endregion

        #region Network

        public bool IsPrivateNetwork()
            => IsPrivateClient;

        public bool IsPublicNetwork()
            => !IsPrivateClient;

        public bool IsConnected()
        {
            if (Disconnected)
                return false;
            return !(Client.Client.Poll(1, SelectMode.SelectRead) && Client.Client.Available == 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Disconnect()
            => Dispose();

        #endregion

        #region Name

        public string GetName()
            => Name;

        #endregion

        #region ToString

        public override string ToString()
        {
            return GetName();
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
