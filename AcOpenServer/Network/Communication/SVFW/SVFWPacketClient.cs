using AcOpenServer.Binary;
using AcOpenServer.Exceptions;
using AcOpenServer.Network.Communication.Tcp;
using AcOpenServer.Network.Data.SVFW;
using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace AcOpenServer.Network.Communication.SVFW
{
    public class SVFWPacketClient : IDisposable
    {
        private const int PacketHeaderSize = 12;
        private readonly NetTcpClient Client;
        private bool ReceivingPrefix;
        private int ExpectingCount;
        private ushort PacketsSent;
        private bool disposedValue;

        public string Name => Client.GetName();
        public bool IsDisposed => disposedValue;
        public bool Disconnected => disposedValue;

        public event EventHandler<SVFWPacket>? Received;

        public SVFWPacketClient(NetTcpClient client)
        {
            Client = client;
            ReceivingPrefix = true;
        }

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
            ExpectingCount = sizeof(ushort);
            Client.Buffer = new byte[ExpectingCount];
            Client.Received += OnReceived;
            return Client.ReceiveAsync();
        }

        public Task SendAsync(SVFWPacket packet)
        {
            packet.Header.SendCounter = ++PacketsSent;
            packet.Header.PayloadLength = (uint)packet.Payload.Length;
            packet.Header.PayloadLengthShort = (ushort)packet.Payload.Length;

            int packetSize = PacketHeaderSize + packet.Payload.Length;
            byte[] buffer = new byte[sizeof(ushort) + packetSize];

            ushort packetLengthPrefix = BinaryPrimitives.ReverseEndianness((ushort)packetSize);
            BinaryBufferWriter.Write(buffer, 0, packetLengthPrefix);

            Write(packet, buffer, sizeof(ushort));
            return Client.SendAsync(buffer);
        }

        #endregion

        #region Disconnect

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Disconnect()
            => Dispose();

        #endregion

        #region Callbacks

        private void OnReceived(object? sender, int received)
        {
            Debug.Assert(Client.Buffer != null);
            if (ReceivingPrefix)
            {
                if (received < ExpectingCount)
                {
                    throw new SVFWPacketException($"Packet prefix length too small; Received: {received}; Minimum Expected: {ExpectingCount}");
                }

                ushort packetLength = BinaryBufferReader.ReadUInt16BigEndian(Client.Buffer);
                if (packetLength < PacketHeaderSize)
                {
                    throw new SVFWPacketException($"Packet is too small to contain a header; Length: {packetLength}; Minimum Expected: {PacketHeaderSize}");
                }

                ExpectingCount = packetLength;
                Client.Buffer = new byte[ExpectingCount];
                ReceivingPrefix = false;
            }
            else
            {
                if (received < ExpectingCount)
                {
                    throw new SVFWPacketException($"Packet data length too small; Received: {received}; Minimum Expected: {ExpectingCount}");
                }

                Received?.Invoke(this, Read(Client.Buffer));

                ExpectingCount = sizeof(ushort);
                Client.Buffer = new byte[ExpectingCount];
                ReceivingPrefix = true;
            }
        }

        #endregion

        #region Serialization

        private static SVFWPacket Read(byte[] buffer)
        {
            var header = BinaryBufferReader.Read<SVFWPacketHeader>(buffer);
            header.SwapEndian();

            var payload = buffer.Length == PacketHeaderSize ? [] : buffer[PacketHeaderSize..];
            if (payload.Length != header.PayloadLength)
            {
                throw new SVFWPacketException($"Detected invalid payload length; Length: {header.PayloadLength}; Expected: {payload.Length}");
            }

            return new SVFWPacket(header, payload);
        }

        private static void Write(SVFWPacket packet, byte[] buffer, int offset)
        {
            var header = packet.Header;
            header.SwapEndian();

            BinaryBufferWriter.Write(buffer, offset, header);
            Array.Copy(packet.Payload, 0, buffer, offset + PacketHeaderSize, packet.Payload.Length);
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
