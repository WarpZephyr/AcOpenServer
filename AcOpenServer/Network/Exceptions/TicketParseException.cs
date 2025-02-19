using System;

namespace AcOpenServer.Network.Exceptions
{
    public class TicketParseException : Exception
    {
        public TicketParseException() : base() { }
        public TicketParseException(string? message) : base(message) { }
        public TicketParseException(string? message, Exception? innerException) : base(message, innerException) { }
        public override string ToString()
        {
            return Message;
        }
    }
}
