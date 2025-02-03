using System;

namespace AcOpenServer.Network.Exceptions
{
    public class AuthException : Exception
    {
        public AuthException() : base() { }
        public AuthException(string? message) : base(message) { }
        public AuthException(string? message, Exception? innerException) : base(message, innerException) { }
        public override string ToString()
        {
            return Message;
        }
    }
}
