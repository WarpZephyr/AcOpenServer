using System;

namespace AcOpenServer.Core.Network
{
    public class NetConnectionEventArgs : EventArgs
    {
        public NetConnection? Connection { get; init; }
        public byte[]? Buffer { get; init; }
        public int ReceivedCount { get; init; }

        public NetConnectionEventArgs(NetConnection? connection)
        {
            Connection = connection;
            Buffer = null;
            ReceivedCount = 0;
        }

        public NetConnectionEventArgs(byte[]? buffer, int receivedCount)
        {
            Connection = null;
            Buffer = buffer;
            ReceivedCount = receivedCount;
        }
    }
}
