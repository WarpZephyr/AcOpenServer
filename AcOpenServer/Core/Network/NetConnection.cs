using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace AcOpenServer.Core.Network
{
    public class NetConnection : IDisposable
    {
        #region Properties

        private readonly string Name;
        private readonly EndPoint? EndPoint;
        private readonly Socket Socket;
        private bool disposedValue;

        public int BackLog { get; set; }

        public bool IsDisposed => disposedValue;

        #endregion

        #region Events

        public event EventHandler<NetConnectionEventArgs>? Accepted;

        #endregion

        #region Constructors

        public NetConnection(string name, string ip, int port) : this(name, IPAddress.Parse(ip), port) { }

        public NetConnection(string name, IPAddress ip, int port)
        {
            Name = name;
            EndPoint = new IPEndPoint(ip, port);
            Socket = new Socket(EndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true
            };

            BackLog = 1024;
        }

        private NetConnection(string name, Socket socket)
        {
            Name = name;
            EndPoint = null;
            Socket = socket;
            BackLog = 1024;
        }

        #endregion

        #region Name

        public string GetName()
            => Name;

        #endregion

        #region Event Starts

        public void Listen()
        {
            if (EndPoint == null)
            {
                throw new Exception("Cannot listen with a client connection.");
            }

            Socket.Bind(EndPoint);
            Socket.Listen(BackLog);
            StartAccept();
        }

        private bool StartAccept()
        {
            // Start accepting state
            var args = GetEventArgs(OnAccepted);
            bool pending = Socket.AcceptAsync(args);
            if (!pending && args.SocketError == SocketError.Success)
            {
                OnAccepted(this, args);
            }

            return pending;
        }

        #endregion

        #region Callbacks

        private void OnAccepted(object? sender, SocketAsyncEventArgs e)
        {
            var socket = e.AcceptSocket;
            if (socket != null)
            {
                var remoteEndPoint = socket.RemoteEndPoint ?? throw new Exception("Remote end point was null on a remote connection.");
                var name = $"{Name}:{remoteEndPoint}";
                var connection = new NetConnection(name, socket);
                var connectionArgs = new NetConnectionEventArgs(connection);
                Accepted?.Invoke(this, connectionArgs);
            }

            // Restart accepting state
            e.AcceptSocket = null;
            bool pending = Socket.AcceptAsync(e);
            if (!pending && e.SocketError == SocketError.Success)
            {
                OnAccepted(sender, e);
            }
        }

        #endregion

        #region IO

        public async Task<int> ReceiveAsync(byte[] buffer)
        {
            return await Socket.ReceiveAsync(buffer);
        }

        public async Task<int> SendAsync(byte[] buffer)
        {
            return await Socket.SendAsync(buffer);
        }

        #endregion

        #region EventArgs

        private static SocketAsyncEventArgs GetEventArgs(EventHandler<SocketAsyncEventArgs> completed)
        {
            var args = new SocketAsyncEventArgs();
            args.Completed += completed;
            return args;
        }

        #endregion

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Socket.Dispose();
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
