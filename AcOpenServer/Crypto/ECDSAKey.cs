using Org.BouncyCastle.Crypto.Parameters;
using System.IO;

namespace AcOpenServer.Crypto
{
    public static class ECDSAKey
    {
        private const string Algorithm = "ECDSA";

        public static ECPublicKeyParameters LoadPublicKeyFromPemFile(string path)
        {
            using var reader = File.OpenText(path);
            using var pemReader = new Org.BouncyCastle.OpenSsl.PemReader(reader);
            var obj = pemReader.ReadObject();

            if (obj is ECPublicKeyParameters key)
            {
                return key;
            }
            else
            {
                throw new InvalidDataException($"Data could not be converted to {nameof(ECPublicKeyParameters)}.");
            }
        }
    }
}
