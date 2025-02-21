using System.Buffers.Binary;

namespace AcOpenServer.Network.Data.FSDP
{
    public struct FsdpFragmentHeader
    {
        public ushort PacketCounter;
        public byte CompressFlag;
        public byte Unk03;
        public byte Unk04;
        public byte Unk05;
        public ushort TotalPayloadLength;
        public byte Unk08;
        public byte FragmentIndex;
        public ushort FragmentLength;

        public void SwapEndian()
        {
            PacketCounter = BinaryPrimitives.ReverseEndianness(PacketCounter);
            TotalPayloadLength = BinaryPrimitives.ReverseEndianness(TotalPayloadLength);
            FragmentLength = BinaryPrimitives.ReverseEndianness(FragmentLength);
        }
    }
}
