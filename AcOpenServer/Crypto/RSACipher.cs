using OpenSSL.Crypto;
using static OpenSSL.Crypto.RSA;

namespace AcOpenServer.Crypto
{
    public class RSACipher : ICipher
    {
        private readonly RSAKey Key;
        private readonly Padding PaddingMode;
        private bool disposedValue;

        public bool IsDisposed => disposedValue;

        public RSACipher(RSAKey key, Padding paddingMode)
        {
            Key = key;
            PaddingMode = paddingMode;
        }

        public byte[] Decrypt(byte[] input)
        {
            RSA rsa = Key.GetRSA();

            byte[] output;
            if (Key.IsPublicKey)
            {
                output = rsa.PublicDecrypt(input, PaddingMode);
            }
            else
            {
                output = rsa.PrivateDecrypt(input, PaddingMode);
            }

            return output;
        }

        public byte[] Encrypt(byte[] input)
        {
            RSA rsa = Key.GetRSA();

            byte[] output;
            if (Key.IsPublicKey)
            {
                output = rsa.PublicEncrypt(input, PaddingMode);
            }
            else
            {
                output = rsa.PrivateEncrypt(input, PaddingMode);
            }

            return output;
        }

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Key.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            System.GC.SuppressFinalize(this);
        }

        #endregion
    }
}
