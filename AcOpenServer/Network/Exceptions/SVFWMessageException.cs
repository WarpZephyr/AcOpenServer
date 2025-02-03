using System;

namespace AcOpenServer.Network.Exceptions
{
    public class SVFWMessageException : Exception
    {
        public SVFWMessageException() : base() { }
        public SVFWMessageException(string? message) : base(message) { }
        public override string ToString()
        {
            return Message;
        }
    }
}
