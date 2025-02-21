using System;

namespace AcOpenServer.Network.Communication.Udp
{
    public class UdpChannelAcceptedEventArgs : EventArgs
    {
        public UdpChannelClient Client { get; set; }
        public byte[] Buffer { get; init; }

        public UdpChannelAcceptedEventArgs(UdpChannelClient client, byte[] buffer)
        {
            Client = client;
            Client.Buffer = buffer;

            Buffer = buffer;
        }
    }
}
