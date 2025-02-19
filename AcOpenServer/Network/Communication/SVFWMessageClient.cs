using AcOpenServer.Binary;
using AcOpenServer.Crypto;
using AcOpenServer.Network.Data.AC;
using AcOpenServer.Network.Exceptions;
using Google.Protobuf;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace AcOpenServer.Network.Communication
{
    public class SVFWMessageClient : IDisposable
    {
        private const int MessageHeaderSize = 12;
        private const int MessageResponseHeaderSize = 16;
        private readonly SVFWPacketClient Client;
        private ICipher EncryptionCipher;
        private ICipher DecryptionCipher;
        private bool disposedValue;

        public bool CipherEnabled { get; set; }
        public string Name => Client.Name;
        public bool IsDisposed => disposedValue;
        public bool Disconnected => disposedValue;

        public event EventHandler<SVFWMessage>? Received;

        public SVFWMessageClient(SVFWPacketClient client, ICipher encryptionCipher, ICipher decryptionCipher)
        {
            Client = client;
            EncryptionCipher = encryptionCipher;
            DecryptionCipher = decryptionCipher;
            CipherEnabled = true;
        }

        #region Cipher

        public void SetCipher(ICipher encryptionCipher, ICipher decryptionCipher)
        {
            EncryptionCipher = encryptionCipher;
            DecryptionCipher = decryptionCipher;
        }

        #endregion

        #region Network

        public bool IsPrivateNetwork()
            => Client.IsPrivateNetwork();

        public bool IsPublicNetwork()
            => Client.IsPublicNetwork();

        public bool IsConnected()
            => Client.IsConnected();

        #endregion

        #region IO

        public Task ReceiveAsync()
        {
            Client.Received += OnReceived;
            return Client.ReceiveAsync();
        }

        public Task SendAsync(SVFWMessage message, SVFWMessageType messageType, uint messageIndex)
        {
            message.Header.MessageType = messageType;
            message.Header.MessageIndex = messageIndex;

            SVFWMessageResponseHeader? responseHeader;
            if (messageType == SVFWMessageType.Reply)
            {
                responseHeader = new SVFWMessageResponseHeader();
            }
            else
            {
                responseHeader = null;
            }

            message.ResponseHeader = responseHeader;

            if (CipherEnabled)
            {
                try
                {
                    message.Payload = EncryptionCipher.Encrypt(message.Payload);
                }
                catch (Exception ex)
                {
                    throw new SVFWMessageException($"Failed to encrypt message payload:\n{ex}");
                }
            }

            return Client.SendAsync(Write(message));
        }

        public Task SendAsync(IMessage protobuf, SVFWMessageType messageType, uint messageIndex)
        {
            byte[] payload = protobuf.ToByteArray();
            var message = new SVFWMessage(payload);
            return SendAsync(message, messageType, messageIndex);
        }

        #endregion

        #region Disconnect

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Disconnect()
            => Dispose();

        #endregion

        #region Callbacks

        private void OnReceived(object? sender, SVFWPacket packet)
        {
            var message = Read(packet.Payload);
            if (CipherEnabled)
            {
                try
                {
                    message.Payload = DecryptionCipher.Decrypt(message.Payload);
                }
                catch (Exception ex)
                {
                    throw new SVFWMessageException($"Failed to decrypt message payload:\n{ex}");
                }
            }

            Received?.Invoke(this, message);
        }

        #endregion

        #region Serialization

        private static SVFWMessage Read(byte[] buffer)
        {
            if (buffer.Length < MessageHeaderSize)
            {
                throw new SVFWMessageException($"Message is too small to have a header; Size: {buffer.Length}; Minimum Expected: {MessageHeaderSize}");
            }

            var header = BinaryBufferReader.Read<SVFWMessageHeader>(buffer, 0);
            header.SwapEndian();

            int payloadOffset = MessageHeaderSize;
            SVFWMessageResponseHeader? responseHeader;
            if (header.MessageType == SVFWMessageType.Reply)
            {
                payloadOffset += MessageResponseHeaderSize;
                if (buffer.Length < payloadOffset)
                {
                    throw new SVFWMessageException($"Message is a reply, but is too small to have a response header; Size: {buffer.Length}; Minimum Expected: {payloadOffset}");
                }

                responseHeader = BinaryBufferReader.Read<SVFWMessageResponseHeader>(buffer, MessageHeaderSize);
                responseHeader.Value.SwapEndian();
            }
            else
            {
                responseHeader = null;
            }

            int payloadLength = buffer.Length - payloadOffset;
            var payload = payloadLength == payloadOffset ? [] : buffer[payloadOffset..];

            return new SVFWMessage(header, responseHeader, payload);
        }

        private static SVFWPacket Write(SVFWMessage message)
        {
            bool isReply = message.Header.MessageType == SVFWMessageType.Reply;
            int messageSize = MessageHeaderSize
                + (isReply ? MessageResponseHeaderSize : 0)
                + message.Payload.Length;

            byte[] buffer = new byte[messageSize];
            var header = message.Header;
            header.SwapEndian();
            BinaryBufferWriter.Write(buffer, 0, header);

            int payloadOffset = MessageHeaderSize;
            if (isReply)
            {
                payloadOffset += MessageResponseHeaderSize;
                var nullableResponseHeader = message.ResponseHeader;
                Debug.Assert(nullableResponseHeader != null);
                var responseHeader = nullableResponseHeader.Value;
                responseHeader.SwapEndian();
                BinaryBufferWriter.Write(buffer, MessageHeaderSize, responseHeader);
            }

            Array.Copy(message.Payload, 0, buffer, payloadOffset, message.Payload.Length);
            return new SVFWPacket(default, buffer);
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
            return Name;
        }

        #endregion
    }
}
