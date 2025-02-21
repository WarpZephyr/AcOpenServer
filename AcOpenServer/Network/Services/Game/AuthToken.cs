using AcOpenServer.Crypto;

namespace AcOpenServer.Network.Services.Game
{
    public class AuthToken
    {
        public required ulong Token;
        public required CWCKey CwcKey;
    }
}
