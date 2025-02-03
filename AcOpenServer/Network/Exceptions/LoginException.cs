using System;

namespace AcOpenServer.Network.Exceptions
{
    public class LoginException : Exception
    {
        public LoginException() : base() { }
        public LoginException(string? message) : base(message) { }
        public LoginException(string? message, Exception? innerException) : base(message, innerException) { }
        public override string ToString()
        {
            return Message;
        }
    }
}
