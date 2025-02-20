namespace AcOpenServer.Network.Data.FSDP
{
    public struct FSDPPacketHeader
    {
        public ushort Magic;
        private byte AckCounter1;
        private byte AckCounter2;
        private byte AckCounter3;
        public FSDPOpcode Opcode;
        public byte Unk06;

        public int LocalAckCounter
        {
            readonly get
            {
                uint upper_nibble = ((uint)AckCounter2 & 0xf0) >> 4;
                return (int)(AckCounter1 | (upper_nibble << 8));
            }
            set => SetAckCounters(value, RemoteAckCounter);
        }

        public int RemoteAckCounter
        {
            readonly get
            {
                uint lower_nibble = ((uint)AckCounter2 & 0x0f);
                return (int)(AckCounter3 | (lower_nibble << 8));
            }
            set => SetAckCounters(LocalAckCounter, value);
        }

        private void SetAckCounters(int local, int remote)
        {
            uint upper_nibble = (uint)((local >> 8) & 0xF);
            uint lower_nibble = (uint)((remote >> 8) & 0xF);

            AckCounter1 = (byte)(local & 0xFF);
            AckCounter2 = (byte)((upper_nibble << 4) | (lower_nibble));
            AckCounter3 = (byte)(remote & 0xFF);
        }
    }
}
