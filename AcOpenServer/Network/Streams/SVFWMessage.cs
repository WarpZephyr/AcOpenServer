namespace AcOpenServer.Network.Streams
{
    public class SVFWMessage
    {
        public SVFWMessageHeader Header;
        public SVFWMessageResponseHeader? ResponseHeader;
        public byte[] Payload;

        public SVFWMessage(byte[] payload)
        {
            Header = new SVFWMessageHeader();
            ResponseHeader = null;
            Payload = payload;
        }

        public SVFWMessage(SVFWMessageHeader header, byte[] payload)
        {
            Header = header;
            ResponseHeader = null;
            Payload = payload;
        }

        public SVFWMessage(SVFWMessageHeader header, SVFWMessageResponseHeader? responseHeader, byte[] payload)
        {
            Header = header;
            ResponseHeader = responseHeader;
            Payload = payload;
        }
    }
}
