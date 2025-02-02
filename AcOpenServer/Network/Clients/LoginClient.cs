using AcOpenServer.Core.Crypto;
using AcOpenServer.Core.Logging;
using AcOpenServer.Core.Network;
using AcOpenServer.Network.Services;
using AcOpenServer.Network.Streams;
using SVFWRequestMessage;
using System;
using System.Threading.Tasks;

namespace AcOpenServer.Network.Clients
{
    public class LoginClient : IDisposable
    {
        private readonly Logger Log;
        private readonly NetConnection Connection;
        private readonly SVFWPacketStream PacketStream;
        private readonly SVFWMessageStream MessageStream;
        private readonly int AuthPort;
        private readonly double Timeout;
        private DateTime LastMessageTime;
        private bool disposedValue;

        public string Name { get; init; }

        public LoginClient(string name, NetConnection connection, RSAKey serverKey, int authPort, double timeout, Logger log)
        {
            Log = log;
            Connection = connection;
            PacketStream = new SVFWPacketStream(Connection, Log);
            MessageStream = new SVFWMessageStream(PacketStream, serverKey, Log);
            AuthPort = authPort;
            Timeout = timeout;
            LastMessageTime = DateTime.Now;

            Name = name;
        }

        public async Task<bool> UpdateAsync()
        {
            if (DateTime.Now.Subtract(LastMessageTime).TotalSeconds >= Timeout)
            {
                Log.Warning($"Client {Name} has timed out.");
                return false;
            }

            if (!await PacketStream.UpdateAsync())
            {
                Log.Warning($"Disconnecting client due to a packet stream error.");
                return false;
            }

            if (!MessageStream.Receive(out SVFWMessage? message, out StreamErrorCode error))
            {
                if (error == StreamErrorCode.Error)
                {
                    Log.Warning($"Disconnecting client due to a message stream error.");
                    return false;
                }

                LastMessageTime = DateTime.Now;
                return true;
            }

            if (message.Header.MessageType != SVFWMessageType.RequestQueryLoginServerInfo)
            {
                Log.Warning($"Disconnecting client {Name} due to unexpected message type: {message.Header.MessageType}.");
                return false;
            }

            RequestQueryLoginServerInfo request;
            try
            {
                request = RequestQueryLoginServerInfo.Parser.ParseFrom(message.Payload);
            }
            catch (Exception ex)
            {
                Log.Warning($"Disconnecting client {Name} due to message parsing failure:\n{ex}");
                return false;
            }

            Log.Info($"User {request.PlayerId} is trying to login.");

            var response = new RequestQueryLoginServerInfoResponse();
            response.Port = AuthPort;

            if (!await MessageStream.SendAsync(response, SVFWMessageType.Reply, message.Header.MessageIndex))
            {
                Log.Warning($"Disconnecting client {Name} due to failure to send {nameof(RequestQueryLoginServerInfoResponse)}.");
                return false;
            }

            Log.Info($"Client {Name} was sent the authentication server details.");
            return true;
        }

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    MessageStream.Dispose();
                    PacketStream.Dispose();
                    Connection.Dispose();
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
