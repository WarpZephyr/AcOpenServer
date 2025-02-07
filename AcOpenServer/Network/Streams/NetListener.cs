using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace AcOpenServer.Network.Streams
{
    public class NetListener : IDisposable
    {
        private readonly TcpListener Listener;
        private bool disposedValue;

        public bool IsDisposed => disposedValue;

        public event EventHandler<NetClient>? Accepted;

        public NetListener(TcpListener listener)
        {
            Listener = listener;
        }

        #region IO

        public async Task ListenAsync()
        {
            Listener.Start();
            while (true)
            {
                var client = await Listener.AcceptTcpClientAsync();
                Accepted?.Invoke(this, new NetClient(client));
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
