using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace AcOpenServer.Core.Network
{
    public class NetClient : IDisposable
    {
        private readonly TcpClient Client;
        private readonly NetworkStream Stream;
        private readonly string Name;
        private bool disposedValue;

        public byte[]? Buffer { get; set; }
        public bool IsDisposed => disposedValue;

        public event EventHandler<int>? Received;

        public NetClient(TcpClient client)
        {
            Client = client;
            Stream = client.GetStream();

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
            while (true)
            {
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
