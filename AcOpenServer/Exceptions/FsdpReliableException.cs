using System;

namespace AcOpenServer.Exceptions
{
    public class FsdpReliableException : Exception
    {
        public FsdpReliableException() : base() { }
        public FsdpReliableException(string? message) : base(message) { }
        public FsdpReliableException(string? message, Exception? innerException) : base(message, innerException) { }
    }
}
