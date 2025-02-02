using System;

namespace AcOpenServer.Core.Exceptions
{
    public class CipherException : Exception
    {
        public CipherException() : base() { }
        public CipherException(string? message) : base(message) { }
    }
}
