using System;

namespace AcOpenServer.Network.Exceptions
{
    public class SVFWPacketException : Exception
    {
        public SVFWPacketException() : base() { }
        public SVFWPacketException(string? message) : base(message) { }
        public override string ToString()
        {
            return Message;
        }
    }
}
