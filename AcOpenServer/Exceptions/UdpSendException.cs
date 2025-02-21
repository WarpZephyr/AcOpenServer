using System;

namespace AcOpenServer.Exceptions
{
    public class UdpSendException : Exception
    {
        public UdpSendException() : base() { }
        public UdpSendException(string? message) : base(message) { }
        public UdpSendException(string? message, Exception? innerException) : base(message, innerException) { }
    }
}
