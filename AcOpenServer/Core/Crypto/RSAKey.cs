using OpenSSL.Core;
using OpenSSL.Crypto;
using System;

namespace AcOpenServer.Core.Crypto
{
    public class RSAKey : IDisposable
    {
        private readonly RSA RsaInstance;
        private bool disposedValue;

        public bool IsPublicKey { get; init; }
        public bool IsDisposed => disposedValue;

        private RSAKey(RSA instance, bool isPublicKey)
        {
            RsaInstance = instance;
            IsPublicKey = isPublicKey;
        }

        public static RSAKey LoadPublicKeyFromString(string pem)
            => LoadFromBio(new BIO(pem), true);

        public static RSAKey LoadPublicKeyFromFile(string path)
            => LoadFromBio(BIO.File(path, "r+"), true);

        public static RSAKey LoadPrivateKeyFromString(string pem)
            => LoadFromBio(new BIO(pem), false);

        public static RSAKey LoadPrivateKeyFromFile(string path)
            => LoadFromBio(BIO.File(path, "r+"), false);

        private static RSAKey LoadFromBio(BIO bio, bool isPublic)
        {
            RSA instance;
            if (isPublic)
            {
                instance = RSA.FromPublicKey(bio);
            }
            else
            {
                instance = RSA.FromPrivateKey(bio);
            }

            return new RSAKey(instance, isPublic);
        }

        public static RSAKey Generate()
        {
            BigNumber bigNum = new BigNumber(RSA.RsaF4);
            RSA rsaInstance = new RSA();
            rsaInstance.GenerateKeys(2048, bigNum, null, null);

            return new RSAKey(rsaInstance, false);
        }

        public void Save(string privatePath, string publicPath)
        {
            BIO bioPublic = BIO.File(publicPath, "w+");
            RsaInstance.WritePrivateKey(bioPublic, null, null, null);
            bioPublic.Dispose();

            if (!IsPublicKey)
            {
                BIO bioPrivate = BIO.File(privatePath, "w+");
                RsaInstance.WritePrivateKey(bioPrivate, null, null, null);
                bioPrivate.Dispose();
            }
        }

        public string GetPrivateString()
        {
            if (IsPublicKey)
            {
                throw new Exception($"Cannot get private key from only the public key.");
            }

            BIO bio = BIO.MemoryBuffer();
            RsaInstance.WritePrivateKey(bio, null, null, null);
            var str = bio.ReadString();
            bio.Dispose();

            return str;
        }

        public string GetPublicString()
        {
            BIO bio = BIO.MemoryBuffer();
            RsaInstance.WritePublicKey(bio);
            var str = bio.ReadString();
            bio.Dispose();

            return str;
        }

        public RSA GetRSA()
            => RsaInstance;

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    RsaInstance.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
