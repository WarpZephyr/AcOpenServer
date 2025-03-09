using Org.BouncyCastle.Security;

namespace AcOpenServer.Crypto
{
    internal static class SignerAlgorithmHelper
    {
        internal static bool IsAlgorithmECDSA(string algorithm)
        {
            if (!algorithm.Contains("ECDSA"))
            {
                return false;
            }

            try
            {
                _ = SignerUtilities.GetSigner(algorithm);
                return true;
            }
            catch (SecurityUtilityException)
            {
                return false;
            }
        }
    }
}
