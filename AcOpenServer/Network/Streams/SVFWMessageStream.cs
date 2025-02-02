using AcOpenServer.Core.Buffers;
using AcOpenServer.Core.Crypto;
using AcOpenServer.Core.Logging;
using Google.Protobuf;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using static OpenSSL.Crypto.RSA;

namespace AcOpenServer.Network.Streams
{
    public class SVFWMessageStream : IDisposable
    {
        private const int MessageHeaderSize = 12;
        private const int MessageResponseHeaderSize = 16;

        private readonly Logger Log;
        private readonly SVFWPacketStream PacketStream;
        private ICipher EncryptionCipher;
        private ICipher DecryptionCipher;
        private bool disposedValue;

        public bool CipherEnabled { get; set; }
        public bool IsDisposed => disposedValue;

        public SVFWMessageStream(SVFWPacketStream packetStream, RSAKey serverKey, Logger log)
        {
            Log = log;
            PacketStream = packetStream;
            EncryptionCipher = new RSACipher(serverKey, Padding.X931);
            DecryptionCipher = new RSACipher(serverKey, Padding.OAEP);
            CipherEnabled = true;
        }

        public void SetCipher(ICipher encryptionCipher, ICipher decryptionCipher)
        {
            EncryptionCipher = encryptionCipher;
            DecryptionCipher = decryptionCipher;
        }

        public bool Receive([NotNullWhen(true)] out SVFWMessage? message, out StreamErrorCode error)
        {
            if (!PacketStream.Recieve(out SVFWPacket? packet))
            {
                message = null;
                error = StreamErrorCode.NotSuccess;
                return false;
            }

            if (!ReadMessage(packet.Payload, out message))
            {
                Log.Error("Failed to parse message.");
                error =  StreamErrorCode.Error;
                return false;
            }

            if (CipherEnabled)
            {
                try
                {
                    message.Payload = DecryptionCipher.Decrypt(message.Payload);
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to decrypt message payload:\n{ex}");
                    error = StreamErrorCode.Error;
                    return false;
                }
            }

            error = StreamErrorCode.Success;
            return true;
        }

        public async Task<bool> SendAsync(SVFWMessage message, SVFWMessageType messageType, uint messageIndex)
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
                    Log.Error($"Failed to encrypt message payload:\n{ex}");
                    Dispose();
                    return false;
                }
            }

            if (!WriteMessage(message, out SVFWPacket? packet))
            {
                Log.Error($"Failed to serialize message to packet.");
                Dispose();
                return false;
            }

            if (!await PacketStream.SendAsync(packet))
            {
                Log.Error("Failed to send message.");
                Dispose();
                return false;
            }

            return true;
        }

        public async Task<bool> SendAsync(IMessage protobuf, SVFWMessageType messageType, uint messageIndex)
        {
            try
            {
                byte[] payload = protobuf.ToByteArray();
                var message = new SVFWMessage(payload);
                return await SendAsync(message, messageType, messageIndex);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to seralize protobuf payload:\n{ex}");
                return false;
            }
        }

        #region Helpers

        private bool ReadMessage(byte[] buffer, [NotNullWhen(true)] out SVFWMessage? message)
        {
            if (buffer.Length < MessageHeaderSize)
            {
                Log.Error($"Message is too small to have a header; Size: {buffer.Length}; Minimum Expected: {MessageHeaderSize}");
                message = null;
                return false;
            }

            var header = BufferReadHelper.Read<SVFWMessageHeader>(buffer, 0);
            header.SwapEndian();

            int payloadOffset = MessageHeaderSize;
            SVFWMessageResponseHeader? responseHeader;
            if (header.MessageType == SVFWMessageType.Reply)
            {
                payloadOffset += MessageResponseHeaderSize;
                if (buffer.Length < payloadOffset)
                {
                    Log.Error($"Message is a reply, but is too small to have a response header; Size: {buffer.Length}; Minimum Expected: {payloadOffset}");
                    message = null;
                    return false;
                }

                responseHeader = BufferReadHelper.Read<SVFWMessageResponseHeader>(buffer, MessageHeaderSize);
                responseHeader.Value.SwapEndian();
            }
            else
            {
                responseHeader = null;
            }

            int payloadLength = buffer.Length - payloadOffset;
            var payload = payloadLength == payloadOffset ? [] : buffer[payloadOffset..];

            message = new SVFWMessage(header, responseHeader, payload);
            return true;
        }

        private bool WriteMessage(SVFWMessage message, [NotNullWhen(true)] out SVFWPacket? packet)
        {
            bool isReply = message.Header.MessageType == SVFWMessageType.Reply;
            int messageSize = MessageHeaderSize
                + (isReply ? MessageResponseHeaderSize : 0)
                + message.Payload.Length;

            byte[] buffer = new byte[messageSize];
            var header = message.Header;
            header.SwapEndian();
            BufferWriteHelper.Write(header, buffer, 0);

            int payloadOffset = MessageHeaderSize;
            if (isReply)
            {
                payloadOffset += MessageResponseHeaderSize;
                var nullableResponseHeader = message.ResponseHeader;
                if (nullableResponseHeader == null)
                {
                    Log.Error("Message is a reply but the response header is null.");
                    packet = null;
                    return false;
                }

                var responseHeader = nullableResponseHeader.Value;
                responseHeader.SwapEndian();
                BufferWriteHelper.Write(responseHeader, buffer, MessageHeaderSize);
            }

            Array.Copy(message.Payload, 0, buffer, payloadOffset, message.Payload.Length);
            packet = new SVFWPacket(default, buffer);
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
                    PacketStream.Dispose();
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
