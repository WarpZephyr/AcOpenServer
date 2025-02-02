namespace AcOpenServer.Network.Streams
{
    public enum SVFWMessageType
    {
        Reply = 0x0,

        // Authentication flow messages.
        KeyMaterial = 1,
        Ticket = 3,
        GetServiceStatus = 2,
        RequestQueryLoginServerInfo = 5,
        RequestHandshake = 6,
    }
}
