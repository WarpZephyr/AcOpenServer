namespace AcOpenServer.Network.Data.FSDP
{
    public class FsdpReliablePacket
    {
        public FsdpReliablePacketHeader Header { get; set; }
        public byte[] Payload { get; set; }

        public FsdpReliablePacket(FsdpReliablePacketHeader header, byte[] payload)
        {
            Header = header;
            Payload = payload;
        }
    }
}
