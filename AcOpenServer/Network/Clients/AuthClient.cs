using AcOpenServer.Core.Crypto;
using AcOpenServer.Core.Logging;
using AcOpenServer.Core.Network;
using AcOpenServer.Core.Utilities;
using AcOpenServer.Network.Streams;
using Google.Protobuf;
using SVFWRequestMessage;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace AcOpenServer.Network.Clients
{
    public class AuthClient : IDisposable
    {
        private readonly Logger Log;
        private readonly NetConnection Connection;
        private readonly SVFWPacketStream PacketStream;
        private readonly SVFWMessageStream MessageStream;
        private readonly int AuthPort;
        private readonly double Timeout;
        private AuthClientState AuthState;
        private DateTime LastMessageTime;
        private bool disposedValue;

        public string Name { get; init; }

        public AuthClient(string name, NetConnection connection, RSAKey serverKey, int authPort, double timeout, Logger log)
        {
            Log = log;
            Connection = connection;
            PacketStream = new SVFWPacketStream(Connection, Log);
            MessageStream = new SVFWMessageStream(PacketStream, serverKey, Log);

            AuthPort = authPort;
            Timeout = timeout;
            AuthState = AuthClientState.WaitingForHandshakeRequest;
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

                Log.Debug($"Waiting on {AuthState} from client {Name}");
                LastMessageTime = DateTime.Now;
                return true;
            }

            switch (AuthState)
            {
                case AuthClientState.WaitingForHandshakeRequest:
                    if (!ValidState<RequestHandshake>(message.Header.MessageType, SVFWMessageType.RequestHandshake))
                    {
                        return false;
                    }

                    if (!ParseMessage(message, out RequestHandshake? handshake))
                    {
                        return false;
                    }

                    var cwcCipher = new CWCCipher(new CWCKey(handshake.AesCwcKey.Memory.ToArray()));
                    MessageStream.SetCipher(cwcCipher, cwcCipher);

                    MessageStream.CipherEnabled = false;
                    byte[] responseBuffer = new byte[27];
                    var rand = new Random();
                    rand.NextBytes(responseBuffer);
                    int remaining = 11 + 16;
                    for (int i = 11; i < remaining; i++)
                        responseBuffer[i] = 0;

                    var handshakeResponseMessage = new SVFWMessage(responseBuffer);
                    if (!await SendMessage<RequestHandshakeResponse>(handshakeResponseMessage))
                    {
                        return false;
                    }

                    Log.Debug($"Sent {nameof(RequestHandshakeResponse)} to client {Name}");
                    MessageStream.CipherEnabled = true;
                    AuthState = AuthClientState.WaitingForServiceStatusRequest;
                    break;
                case AuthClientState.WaitingForServiceStatusRequest:
                    if (!ValidState<GetServiceStatus>(message.Header.MessageType, SVFWMessageType.GetServiceStatus))
                    {
                        return false;
                    }

                    if (!ParseMessage(message, out GetServiceStatus? serviceStatus))
                    {
                        return false;
                    }

                    var serviceStatusResponse = new GetServiceStatusResponse();
                    serviceStatusResponse.PlayerId = serviceStatus.PlayerId;
                    serviceStatusResponse.Id = serviceStatus.Id;
                    serviceStatusResponse.Unknown1 = 0;
                    serviceStatusResponse.AppVersion = serviceStatus.AppVersion;

                    if (!await SendMessage<GetServiceStatusResponse>(serviceStatusResponse, message.Header.MessageIndex))
                    {
                        return false;
                    }

                    AuthState = AuthClientState.Complete;
                    break;
                case AuthClientState.Complete:
                    Log.Debug(message.Header.MessageType.ToString());
                    Log.Debug(message.Payload.ToHexView(0x10));
                    break;
            }

            return true;
        }

        #region Helpers

        private bool ValidState<T>(SVFWMessageType type, SVFWMessageType expectedType)
        {
            if (type != expectedType)
            {
                Log.Warning($"Disconnecting client {Name} due to an invalid message for {typeof(T).Name}: {type}");
                return false;
            }

            return true;
        }

        private bool ParseMessage<T>(SVFWMessage message, [NotNullWhen(true)] out T? result) where T : IMessage, new()
        {
            string typeName = typeof(T).Name;
            Log.Info($"Client {Name} has sent {typeName}.");

            try
            {
                result = ProtobufHelper.ParseFrom<T>(message.Payload);
            }
            catch (Exception ex)
            {
                Log.Warning($"Disconnecting client {Name} due to a {typeName} parsing failure:\n{ex}");
                result = default;
                return false;
            }

            return true;
        }

        private async Task<bool> SendMessage<T>(SVFWMessage message)
        {
            if (!await MessageStream.SendAsync(message, SVFWMessageType.Reply, message.Header.MessageIndex))
            {
                Log.Warning($"Disconnecting client {Name} due to failure to send {typeof(T).Name}.");
                return false;
            }

            return true;
        }

        private async Task<bool> SendMessage<T>(IMessage message, uint messageIndex)
        {
            if (!await MessageStream.SendAsync(message, SVFWMessageType.Reply, messageIndex))
            {
                Log.Warning($"Disconnecting client {Name} due to failure to send {typeof(T).Name}.");
                return false;
            }

            return true;
        }

        #endregion

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
