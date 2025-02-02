using AcOpenServer.Core.Crypto;
using OpenSSL.Crypto;

namespace AcOpenServer.Tests
{
    public class RsaTest
    {
        private readonly RSAKey Key;
        private readonly RSACipher Cipher;
        public bool IsPublic { get; init; }
        public bool IsPrivate { get; init; }
        public bool DoEncrypt { get; init; }
        public bool DoDecrypt { get; init; }

        public RsaTest(string key, bool isPublic, bool doEncrypt)
        {
            IsPublic = isPublic;
            IsPrivate = !isPublic;
            DoEncrypt = doEncrypt;
            DoDecrypt = !doEncrypt;

            RSA.Padding paddingMode = (IsPublic && DoDecrypt) || (IsPrivate && DoEncrypt) ? RSA.Padding.X931 : RSA.Padding.OAEP;
            if (isPublic)
            {
                Key = RSAKey.LoadPublicKeyFromString(key);
            }
            else
            {
                Key = RSAKey.LoadPrivateKeyFromString(key);
            }

            Cipher = new RSACipher(Key, paddingMode);
        }

        public byte[] Run(byte[] input)
        {
            if (DoDecrypt)
            {
                return Cipher.Decrypt(input);
            }
            else
            {
                return Cipher.Encrypt(input);
            }
        }
    }
}
