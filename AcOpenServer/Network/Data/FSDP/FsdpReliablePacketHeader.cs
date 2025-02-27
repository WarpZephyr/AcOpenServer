namespace AcOpenServer.Network.Data.FSDP
{
    public struct FsdpReliablePacketHeader
    {
        public const int Length = sizeof(ushort)
            + sizeof(byte)
            + sizeof(byte)
            + sizeof(byte)
            + sizeof(FsdpOpcode)
            + sizeof(byte);

        private ushort Magic;
        private byte Ack1;
        private byte Ack2;
        private byte Ack3;
        public FsdpOpcode Opcode;
        public byte Unk06;

        public int LocalAck
        {
            readonly get
            {
                uint upper_nibble = ((uint)Ack2 & 0xf0) >> 4;
                return (int)(Ack1 | (upper_nibble << 8));
            }
            set => SetAckCounters(value, RemoteAck);
        }

        public int RemoteAck
        {
            readonly get
            {
                uint lower_nibble = ((uint)Ack2 & 0x0f);
                return (int)(Ack3 | (lower_nibble << 8));
            }
            set => SetAckCounters(LocalAck, value);
        }

        private void SetAckCounters(int local, int remote)
        {
            uint upper_nibble = (uint)((local >> 8) & 0xF);
            uint lower_nibble = (uint)((remote >> 8) & 0xF);

            Ack1 = (byte)(local & 0xFF);
            Ack2 = (byte)((upper_nibble << 4) | (lower_nibble));
            Ack3 = (byte)(remote & 0xFF);
        }

        public FsdpReliablePacketHeader()
        {
            Magic = 0x02F5;
        }

        public static FsdpReliablePacketHeader CreateDefault()
        {
            return new FsdpReliablePacketHeader()
            {
                Ack1 = 0,
                Ack2 = 0,
                Ack3 = 0,
                Opcode = FsdpOpcode.UNKNOWN,
                Unk06 = 0xFF
            };
        }
    }
}
