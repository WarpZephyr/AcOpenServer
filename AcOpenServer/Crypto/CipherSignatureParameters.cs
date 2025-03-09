using Org.BouncyCastle.Crypto;

namespace AcOpenServer.Crypto
{
    public record CipherSignatureParameters
    {
        public required string Algorithm { get; init; }
        public required ICipherParameters Parameters { get; init; }
    }
}
