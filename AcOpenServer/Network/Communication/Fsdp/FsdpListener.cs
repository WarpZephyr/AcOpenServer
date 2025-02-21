using AcOpenServer.Network.Communication.Udp;
using System;
using System.Threading.Tasks;

namespace AcOpenServer.Network.Communication.Fsdp
{
    public class FsdpListener
    {
        private readonly UdpChannelListener Listener;

        public event EventHandler<FsdpClient>? Accepted;

        public FsdpListener(UdpChannelListener listener)
        {
            Listener = listener;
        }

        public Task ListenAsync()
        {
            Listener.Accepted += OnAccepted;
            return Listener.ListenAsync();
        }

        private void OnAccepted(object? sender, UdpChannelAcceptedEventArgs client)
        {
            
        }
    }
}
