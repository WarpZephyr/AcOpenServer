namespace AcOpenServer.Network.Data.FSDP
{
    public class FsdpReliablePacketInitialData
    {
        public const int Length = 17 + 1 + 17;
        public string PlayerName { get; set; }
        public byte Unk12 { get; set; }
        public string PlayerNameCopy { get; set; }

        public FsdpReliablePacketInitialData(string playerName, byte unk12, string playerNameCopy)
        {
            PlayerName = playerName;
            Unk12 = unk12;
            PlayerNameCopy = playerNameCopy;
        }
    }
}
