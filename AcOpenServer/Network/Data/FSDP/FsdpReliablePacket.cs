using System;
using System.Runtime.CompilerServices;

namespace AcOpenServer.Network.Data.FSDP
{
    public class FsdpReliablePacket
    {
        #region Header Properties

        /// <summary>
        /// How many values ACK increases before it rolls over.<br/>
        /// Each sequence value is only 12-bits, so (2^12)-1 or 4095 is the max they can be.
        /// </summary>
        private const int MaxAckValue = 1 << 12;

        /// <summary>
        /// The header of the packet.
        /// </summary>
        public FsdpReliablePacketHeader Header { get; set; }

        /// <summary>
        /// Cache to avoid calculation time.
        /// </summary>
        private int LocalAckCache;

        /// <summary>
        /// Cache to avoid calculation time.
        /// </summary>
        private int RemoteAckCache;

        /// <summary>
        /// The local ack of the header.
        /// </summary>
        public int LocalAck
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => LocalAckCache;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                var header = Header;
                header.LocalAck = value;
                LocalAckCache = value % MaxAckValue; // Avoid get calculation
                Header = header;
            }
        }

        /// <summary>
        /// The remote ack of the header.
        /// </summary>
        public int RemoteAck
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => RemoteAckCache;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                var header = Header;
                header.RemoteAck = value;
                RemoteAckCache = value % MaxAckValue; // Avoid get calculation
                Header = header;
            }
        }

        /// <summary>
        /// The opcode of the header.
        /// </summary>
        public FsdpOpcode Opcode
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Header.Opcode;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                var header = Header;
                header.Opcode = value;
                Header = header;
            }
        }

        /// <summary>
        /// The <see cref="FsdpReliablePacketHeader.Unk06"/> value of the header.
        /// </summary>
        public byte Unk06
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Header.Unk06;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                var header = Header;
                header.Unk06 = value;
                Header = header;
            }
        }

        #endregion

        /// <summary>
        /// The payload of the packet.
        /// </summary>
        public byte[] Payload { get; set; }

        /// <summary>
        /// Used for internal book keeping by the server.
        /// </summary>
        private DateTime SendTime;

        public FsdpReliablePacket(FsdpReliablePacketHeader header, byte[] payload)
        {
            Header = header;
            Payload = payload;
            LocalAckCache = header.LocalAck;
            RemoteAckCache = header.RemoteAck;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateSendTime()
            => SendTime = DateTime.UtcNow;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TimeSpan GetTimeSinceSend()
            => DateTime.UtcNow.Subtract(SendTime);
    }
}
