using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using System;

namespace AcOpenServer.Crypto
{
    public static class SignatureVerifier
    {
        public static bool SignatureValid(ISigner signer, ReadOnlySpan<byte> input, byte[] signature)
        {
            signer.BlockUpdate(input);

            // TODO: Trim Signature carefully in case of extra null bytes
            return signer.VerifySignature(signature);
        }

        public static bool SignatureValid(CipherSignatureParameters parameters, ReadOnlySpan<byte> input, byte[] signature)
        {
            ISigner signer = SignerUtilities.GetSigner(parameters.Algorithm);
            signer.Init(false, parameters.Parameters);
            return SignatureValid(signer, input, signature);
        }
    }
}
