using AcOpenServer.Network.Data.AC;

namespace AcOpenServer.Network.Services.Authentication
{
    public class AuthConfig
    {
        public required uint PublicIP { get; init; }
        public required uint PrivateIP { get; init; }
        public required ushort GamePort { get; init; }
        public required AcvAppVersion MinimumVersion { get; init; }
        public required AcvAppVersion MaximumVersion { get; init; }
    }
}
