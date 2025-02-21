using AcOpenServer.Logging;
using System;
using System.Net;
using System.Threading.Tasks;

namespace AcOpenServer.Network.Communication.Udp
{
    public class UdpChannelClient : IDisposable
    {
        private readonly UdpChannelListener Parent;
        private readonly ScopeLog Log;
        private bool Disconnected;
        private bool disposedValue;

        public IPEndPoint EndPoint { get; init; }
        public string Name { get; init; }
        public byte[]? Buffer { get; set; }
        public bool IsDisposed => disposedValue;

        public event EventHandler<int>? Received;

        public UdpChannelClient(UdpChannelListener parent, IPEndPoint endPoint, string name, ScopeLog log)
        {
            Parent = parent;
            EndPoint = endPoint;
            Name = name;
            Log = log;
        }

        #region IO

        public Task SendAsync(byte[] buffer)
        {
            if (Disconnected)
            {
                return Task.CompletedTask;
            }

            return Parent.SendAsync(this, buffer);
        }

        #endregion

        #region Client

        public void NotifyReceived(byte[] buffer)
        {
            if (Disconnected)
            {
                return;
            }

            Buffer = buffer;
            Received?.Invoke(this, buffer.Length);
        }

        public void Disconnect()
        {
            if (Disconnected)
            {
                return;
            }

            Disconnected = true;
            Parent.NotifyClientDisconnect(this);
        }

        #endregion

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Disconnect();
                    Buffer = null;
                    Received = null;
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
