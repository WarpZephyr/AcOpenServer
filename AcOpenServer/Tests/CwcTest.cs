using AcOpenServer.Core.Crypto;

namespace AcOpenServer.Tests
{
    public class CwcTest
    {
        private readonly CWCCipher Cipher;
        public bool DoEncrypt { get; init; }
        public bool DoDecrypt { get; init; }

        public CwcTest(byte[] key, bool doEncrypt)
        {
            DoEncrypt = doEncrypt;
            DoDecrypt = !doEncrypt;

            var cwcKey = new CWCKey(key);
            Cipher = new CWCCipher(cwcKey);
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
