using System.Buffers.Binary;

namespace AcOpenServer.Network.Data.AC
{
    public struct ConnectGameServerPortIdResponse
    {
        public ulong AuthToken;
        public uint Address;
        public ushort GamePort;
        private ushort Padding;
        public uint SendBufferSize;
        public uint ReceiveBufferSize;
        public uint Unk18;
        public uint Unk1C;
        public uint Unk20;
        public uint Unk24;
        public uint Unk28;
        public uint Unk2C;
        public uint Unk30;
        public uint Unk34;

        public void SwapEndian()
        {
            AuthToken = BinaryPrimitives.ReverseEndianness(AuthToken);
            Address = BinaryPrimitives.ReverseEndianness(Address);
            GamePort = BinaryPrimitives.ReverseEndianness(GamePort);
            SendBufferSize = BinaryPrimitives.ReverseEndianness(SendBufferSize);
            ReceiveBufferSize = BinaryPrimitives.ReverseEndianness(ReceiveBufferSize);
            Unk18 = BinaryPrimitives.ReverseEndianness(Unk18);
            Unk1C = BinaryPrimitives.ReverseEndianness(Unk1C);
            Unk20 = BinaryPrimitives.ReverseEndianness(Unk20);
            Unk24 = BinaryPrimitives.ReverseEndianness(Unk24);
            Unk28 = BinaryPrimitives.ReverseEndianness(Unk28);
            Unk2C = BinaryPrimitives.ReverseEndianness(Unk2C);
            Unk30 = BinaryPrimitives.ReverseEndianness(Unk30);
            Unk34 = BinaryPrimitives.ReverseEndianness(Unk34);
        }
    }
}
