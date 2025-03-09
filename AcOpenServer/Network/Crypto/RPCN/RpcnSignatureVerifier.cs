using AcOpenServer.Crypto;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;
using System;

namespace AcOpenServer.Network.Crypto.RPCN
{
    public static class RpcnSignatureVerifier
    {
        private static readonly Lazy<ECPublicKeyParameters> RpcnPublicKey = new(() =>
        {
            X9ECParameters param = ECNamedCurveTable.GetByName("secp224k1");
            ECDomainParameters curve = new(param.Curve, param.G, param.N, param.H, param.GetSeed());
            ECPoint publicKey = curve.Curve.CreatePoint(
                new BigInteger("b07bc0f0addb97657e9f389039e8d2b9c97dc2a31d3042e7d0479b93", 16),
                new BigInteger("d81c42b0abdf6c42191a31e31f93342f8f033bd529c2c57fdb5a0a7d", 16));
            return new ECPublicKeyParameters(publicKey, curve);
        });

        private static readonly Lazy<CipherSignatureParameters> RpcnParameters = new(() =>
        {
            return new CipherSignatureParameters()
            {
                Algorithm = "SHA-224withECDSA",
                Parameters = RpcnPublicKey.Value
            };
        });

        public static CipherSignatureParameters GetRpcnParameters()
            => RpcnParameters.Value;

        public static bool RpcnSignatureValid(ReadOnlySpan<byte> input, byte[] signature)
            => SignatureVerifier.SignatureValid(RpcnParameters.Value, input, signature);
    }
}
