using AcOpenServer.Logging;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace AcOpenServer.Network.Streams
{
    public class NetClient : IDisposable
    {
        private readonly Logger Log;
        private readonly TcpClient Client;
        private readonly NetworkStream Stream;
        private readonly string Name;
        private readonly double Timeout;
        private bool disposedValue;

        public byte[]? Buffer { get; set; }
        public bool IsDisposed => disposedValue;

        public event EventHandler<int>? Received;

        public NetClient(TcpClient client, double timeout, Logger log)
        {
            Log = log;
            Client = client;
            Stream = client.GetStream();
            Timeout = timeout;

            EndPoint remoteEndPoint = client.Client.RemoteEndPoint ?? throw new Exception("Remote end point was null on a remote connection.");
            Name = $"{remoteEndPoint}";
        }

        #region Name

        public string GetName()
            => Name;

        #endregion

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
                            Log.Warning($"Client {Name} has disconnected.");
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
                    Log.Warning($"Client {Name} has timed out.");
                    Client.Close();
                }
            }
            else
            {
                while (true)
                {
                    if (!Client.Connected)
                    {
                        Log.Warning($"Client {Name} has disconnected.");
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

        #region ToString

        public override string ToString()
        {
            return GetName();
        }

        #endregion
    }
}
