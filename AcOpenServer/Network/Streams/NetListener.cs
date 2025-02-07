using AcOpenServer.Logging;
using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace AcOpenServer.Network.Streams
{
    public class NetListener : IDisposable
    {
        private readonly Logger Log;
        private readonly TcpListener Listener;
        private readonly double ClientTimeout;
        private bool disposedValue;

        public bool IsDisposed => disposedValue;

        public event EventHandler<NetClient>? Accepted;

        public NetListener(TcpListener listener, double clientTimeout, Logger log)
        {
            Log = log;
            Listener = listener;
            ClientTimeout = clientTimeout;
        }

        #region IO

        public async Task ListenAsync()
        {
            Listener.Start();
            while (true)
            {
                var client = await Listener.AcceptTcpClientAsync();
                Accepted?.Invoke(this, new NetClient(client, ClientTimeout, Log));
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
