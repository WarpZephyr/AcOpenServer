using System;

namespace AesModesNet
{
    public class CwcException : Exception
    {
        public CwcException() : base() { }

        public CwcException(string? message) : base(message) { }
    }
}
