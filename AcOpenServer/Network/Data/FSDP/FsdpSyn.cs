namespace AcOpenServer.Network.Data.FSDP
{
    public struct FsdpSyn
    {
        public const int Length = sizeof(byte)
            + sizeof(byte)
            + sizeof(byte)
            + sizeof(byte)
            + sizeof(byte)
            + sizeof(byte)
            + sizeof(byte)
            + sizeof(byte);

        public byte Unk00;
        public byte Unk01;
        public byte Unk02;
        public byte Unk03;
        public byte Unk04;
        public byte Unk05;
        public byte Unk06;
        public byte Unk07;

        public static FsdpSyn CreateDefault()
        {
            return new FsdpSyn
            {
                Unk00 = 0x12,
                Unk01 = 0x10,
                Unk02 = 0x20,
                Unk03 = 0x20,
                Unk04 = 0x00,
                Unk05 = 0x00,
                Unk06 = 0xA0,
                Unk07 = 0x00
            };
        }
    }
}
