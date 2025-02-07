using System.Buffers.Binary;

namespace AcOpenServer.Network.Data.AC
{
    public struct SVFWMessageResponseHeader
    {
        public uint Unk00;
        public uint Unk04;
        public uint Unk08;
        public uint Unk0C;

        public SVFWMessageResponseHeader()
        {
            Unk00 = 0;
            Unk04 = 1;
            Unk08 = 0;
            Unk0C = 0;
        }

        public void SwapEndian()
        {
            Unk00 = BinaryPrimitives.ReverseEndianness(Unk00);
            Unk04 = BinaryPrimitives.ReverseEndianness(Unk04);
            Unk08 = BinaryPrimitives.ReverseEndianness(Unk08);
            Unk0C = BinaryPrimitives.ReverseEndianness(Unk0C);
        }
    }
}
