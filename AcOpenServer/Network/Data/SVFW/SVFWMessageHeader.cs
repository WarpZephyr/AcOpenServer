using System.Buffers.Binary;

namespace AcOpenServer.Network.Data.SVFW
{
    public struct SVFWMessageHeader
    {
        public uint HeaderSize;
        public SVFWMessageType MessageType;
        public uint MessageIndex;

        public SVFWMessageHeader()
        {
            HeaderSize = 12;
            MessageType = SVFWMessageType.Reply;
            MessageIndex = 0;
        }

        public void SwapEndian()
        {
            HeaderSize = BinaryPrimitives.ReverseEndianness(HeaderSize);
            MessageType = (SVFWMessageType)BinaryPrimitives.ReverseEndianness((int)MessageType);
            MessageIndex = BinaryPrimitives.ReverseEndianness(MessageIndex);
        }
    }
}
