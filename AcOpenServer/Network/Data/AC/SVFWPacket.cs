namespace AcOpenServer.Network.Data.AC
{
    public class SVFWPacket
    {
        public SVFWPacketHeader Header;
        public byte[] Payload;

        public SVFWPacket(SVFWPacketHeader header, byte[] payload)
        {
            Header = header;
            Payload = payload;
        }
    }
}
