using AcOpenServer.Core.Buffers;
using AcOpenServer.Core.Logging;
using AcOpenServer.Core.Network;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace AcOpenServer.Network.Streams
{
    public class SVFWPacketStream : IDisposable
    {
        private const int PacketHeaderSize = 12;
        private readonly Logger Log;
        private readonly NetConnection Connection;
        private readonly Queue<SVFWPacket> ReceivedQueue;
        private byte[] PacketBuffer;
        private ushort PacketsSent;
        private bool disposedValue;

        public bool IsDisposed => disposedValue;
        
        public SVFWPacketStream(NetConnection connection, Logger log)
        {
            Log = log;
            ReceivedQueue = [];
            PacketBuffer = new byte[sizeof(ushort)];
            PacketsSent = 0;

            Connection = connection;
        }

        public async Task<bool> UpdateAsync()
        {
            bool receivingHeader = true;

            try
            {
                int received;
                while ((received = await Connection.ReceiveAsync(PacketBuffer)) > 0)
                {
                    if (receivingHeader)
                    {
                        ushort packetLength = BinaryPrimitives.ReverseEndianness(BufferReadHelper.Read<ushort>(PacketBuffer));
                        if (packetLength == 0)
                        {
                            Log.Error($"Received invalid packet length of {0}.");
                            return false;
                        }

                        PacketBuffer = new byte[packetLength];
                        receivingHeader = false;
                    }
                    else
                    {
                        if (!ReadPacket(PacketBuffer, out SVFWPacket? packet))
                        {
                            Log.Error("Failed to parse packet.");
                            return false;
                        }

                        ReceivedQueue.Enqueue(packet);
                        PacketBuffer = new byte[sizeof(ushort)];
                        break;
                    }
                }
            }
            catch (SocketException ex)
            {
                Log.Error($"Failed to receive packet due to socket error: {ex.SocketErrorCode}");
                return false;
            }

            return true;
        }

        public bool Recieve([NotNullWhen(true)] out SVFWPacket? packet)
            => ReceivedQueue.TryDequeue(out packet);

        public async Task<bool> SendAsync(SVFWPacket packet)
        {
            packet.Header.SendCounter = ++PacketsSent;
            packet.Header.PayloadLength = (uint)packet.Payload.Length;
            packet.Header.PayloadLengthShort = (ushort)packet.Payload.Length;

            int packetSize = PacketHeaderSize + packet.Payload.Length;
            byte[] buffer = new byte[sizeof(ushort) + packetSize];

            ushort packetLengthPrefix = BinaryPrimitives.ReverseEndianness((ushort)packetSize);
            BufferWriteHelper.Write(packetLengthPrefix, buffer, 0);

            WritePacket(packet, buffer, sizeof(ushort));

            try
            {
                int sent = await Connection.SendAsync(buffer);
                if (sent != buffer.Length)
                {
                    Log.Error($"Failed to send full packet; Sent: {sent}; Expected: {buffer.Length}");
                    return false;
                }

                return true;
            }
            catch (SocketException ex)
            {
                Log.Error($"Failed to send packet due to socket error: {ex.SocketErrorCode}");
                return false;
            }
        }

        private void OnReceived(object? sender, NetConnectionEventArgs e)
        {
            var buffer = e.Buffer;
            var count = e.ReceivedCount;
            if (buffer != null && count > 0)
            {
                Parse(buffer);
            }
        }

        private void Parse(byte[] buffer)
        {
            ushort packetLength = BinaryPrimitives.ReverseEndianness(BufferReadHelper.Read<ushort>(buffer));
            if (packetLength == 0)
            {
                Log.Error($"Received invalid packet length of {0}.");
                return;
            }

            if (packetLength > (buffer.Length - sizeof(ushort)))
            {
                Log.Error($"Received invalid buffer length of {buffer.Length - sizeof(ushort)}; Expected: {packetLength}.");
                return;
            }

            if (!ReadPacket(buffer[sizeof(ushort)..(packetLength+sizeof(ushort))], out SVFWPacket? packet))
            {
                Log.Error("Failed to parse packet.");
                return;
            }

            ReceivedQueue.Enqueue(packet);
        }

        #region Helpers

        private bool ReadPacket(byte[] buffer, [NotNullWhen(true)] out SVFWPacket? packet)
        {
            if (buffer.Length < PacketHeaderSize)
            {
                packet = null;
                Log.Error($"Packet is too small to contain a header; Length: {buffer.Length}; Minimum Expected: {PacketHeaderSize}");
                return false;
            }

            var header = BufferReadHelper.Read<SVFWPacketHeader>(buffer);
            header.SwapEndian();

            var payload = buffer.Length == PacketHeaderSize ? [] : buffer[PacketHeaderSize..];
            if (payload.Length != header.PayloadLength)
            {
                packet = null;
                Log.Error($"Detected invalid payload length; Length: {header.PayloadLength}; Expected: {payload.Length}");
                return false;
            }

            packet = new SVFWPacket(header, payload);
            return true;
        }

        private void WritePacket(SVFWPacket packet, byte[] buffer, int offset)
        {
            var header = packet.Header;
            header.SwapEndian();

            BufferWriteHelper.Write(header, buffer, offset);
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
