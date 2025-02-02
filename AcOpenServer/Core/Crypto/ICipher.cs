using System;

namespace AcOpenServer.Core.Crypto
{
    public interface ICipher : IDisposable
    {
        public byte[] Decrypt(byte[] input);
        public byte[] Encrypt(byte[] input);
    }
}
