using System.Buffers.Binary;

namespace AcOpenServer.Network.Streams
{
    public struct SVFWPacketHeader
    {
        public ushort SendCounter;
        private ushort Unk02;
        public uint PayloadLength;
        private ushort Unk08;
        public ushort PayloadLengthShort;

        public void SwapEndian()
        {
            SendCounter = BinaryPrimitives.ReverseEndianness(SendCounter);
            PayloadLength = BinaryPrimitives.ReverseEndianness(PayloadLength);
            PayloadLengthShort = BinaryPrimitives.ReverseEndianness(PayloadLengthShort);
        }
    }
}
