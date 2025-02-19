using AcOpenServer.Crypto;
using AcOpenServer.Logging;
using AcOpenServer.Network.Data.AC;
using AcOpenServer.Network.Communication;
using AcOpenServer.Utilities;
using OpenSSL.Crypto;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using AcOpenServer.Network.Services.Login;
using AcOpenServer.Network.Services.Authentication;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace AcOpenServer.Network.Servers
{
    public class Server
    {
        private const string ConfigFileName = "config.json";
        private const string PublicKeyFileName = "publickey.pem";
        private const string PrivateKeyFileName = "privatekey.pem";

        private static readonly AcvAppVersion MinimumAppVersion = new AcvAppVersion(0x5644000001000002);
        private static readonly AcvAppVersion MaximumAppVersion = new AcvAppVersion(0x5644000001000002);

        private readonly ScopeLog Log;
        private readonly string ServerFolder;
        private ServerConfig? Config;
        private RSAKey? PrivateKey;
        private IPAddress? PublicIP;
        private IPAddress? PrivateIP;

        public string Name { get; init; }

        public Server(string serverFolder, string name, ScopeLog log)
        {
            Log = log;
            ServerFolder = serverFolder;

            Name = name;
        }

        public Server(string serverFolder, string name, RSAKey privateKey, ServerConfig config, ScopeLog log)
        {
            Log = log;
            ServerFolder = serverFolder;
            PrivateKey = privateKey;
            Config = config;

            Name = name;
        }

        public async Task<bool> StartAsync()
        {
            if (!Directory.Exists(ServerFolder))
            {
                Log.Error("Server folder doesn't exist.");
                return false;
            }

            if (Config == null)
            {
                string configPath = Path.Combine(ServerFolder, ConfigFileName);
                if (!File.Exists(configPath))
                {
                    Log.Warn($"Could not find {ConfigFileName} for {Name} server, making a default config.");
                    Config = new ServerConfig();
                    Config.Save(configPath);
                }
                else if (!ServerConfig.Load(configPath, out ServerConfig? config))
                {
                    Log.Warn($"Failed to load {ConfigFileName} for {Name} server, using default config.");
                    Config = new ServerConfig();
                }
                else
                {
                    Config = config;
                }
            }

#if DEBUG
            Log.ChannelFlags = LogChannelFlags.Debug;
#else
            Log.ChannelFlags = LogChannelFlags.None;
#endif
            if (Config.LogInfo)
            {
                Log.ChannelFlags |= LogChannelFlags.Info;
            }

            if (Config.LogWarnings)
            {
                Log.ChannelFlags |= LogChannelFlags.Warn;
            }

            Log.ChannelFlags |= LogChannelFlags.Error;

            if (PrivateKey == null)
            {
                if (!LoadKey(PrivateKeyFileName, false, out RSAKey? privateKey))
                {
                    Log.Error($"Failed to load private key from {PrivateKeyFileName}");
                    return false;
                }

                PrivateKey = privateKey;
            }

            if (Config.Local)
            {
                var ip = new IPAddress([127, 0, 0, 1]);
                PublicIP = ip;
                PrivateIP = ip;
            }
            else
            {
                PublicIP = await ResolveHostnameAsync(Config.PublicHostname, true);
                if (PublicIP == null)
                {
                    Log.Error($"Failed to resolve public hostname: {Config.PublicHostname}");
                    return false;
                }

                PrivateIP = await ResolveHostnameAsync(Config.PrivateHostname, false);
                if (PrivateIP == null)
                {
                    Log.Error($"Failed to resolve private hostname: {Config.PrivateHostname}");
                    return false;
                }
            }

            Log.Info($"Public ip: {PublicIP}");
            Log.Info($"Private ip: {PrivateIP}");

            if (!IPAddressHelper.TryGetIPV4UInt32(PublicIP, out uint publicIP))
            {
                Log.Error("Failed to get numeric representation of public IP address.");
                return false;
            }

            if (!IPAddressHelper.TryGetIPV4UInt32(PrivateIP, out uint privateIP))
            {
                Log.Error("Failed to get numeric representation of private IP address.");
                return false;
            }

            var listenIP = new IPAddress([0, 0, 0, 0]);
            var loginListener = CreateListener(listenIP, Config.LoginPort, PrivateKey, Config.LoginClientTimeout);
            var authListener = CreateListener(listenIP, Config.AuthPort, PrivateKey, Config.AuthClientTimeout);
            var loginConfig = new LoginConfig()
            {
                AuthPort = (uint)Config.AuthPort
            };

            var authConfig = new AuthConfig()
            {
                PublicIP = publicIP,
                PrivateIP = privateIP,
                GamePort = (ushort)Config.GamePort,
                MinimumVersion = MinimumAppVersion,
                MaximumVersion = MaximumAppVersion
            };

            var loginService = new LoginService(loginListener, loginConfig, Log.Push(nameof(LoginService)));
            var authService = new AuthService(authListener, authConfig, Log.Push(nameof(AuthService)));
            var loginTask = loginService.ListenAsync();
            var authTask = authService.ListenAsync();
            await Task.WhenAll(loginTask, authTask);
            return true;
        }

        #region Cryptography

        private bool LoadKey(string filename, bool isPublic, [NotNullWhen(true)] out RSAKey? key)
        {
            string path = Path.Combine(ServerFolder, filename);
            if (!File.Exists(path))
            {
                Log.Error($"Failed to find key {filename} for {Name} server.");
                key = null;
                return false;
            }

            try
            {
                key = isPublic ? RSAKey.LoadPublicKeyFromFile(path) : RSAKey.LoadPrivateKeyFromFile(path);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to load key {filename} for {Name} server:\n{ex}");
                key = null;
                return false;
            }
        }

        #endregion

        #region Network

        private SVFWMessageListener CreateListener(IPAddress serverIP, int port, RSAKey key, double clientTimeout)
        {
            var tcpListener = new TcpListener(serverIP, port);
            var netListener = new NetTcpListener(tcpListener, clientTimeout, Log.Push(nameof(NetTcpListener)));

            var decryptionCipher = new RSACipher(key, RSA.Padding.OAEP);
            var encryptionCipher = new RSACipher(key, RSA.Padding.X931);
            var messageListener = new SVFWMessageListener(netListener, encryptionCipher, decryptionCipher);
            return messageListener;
        }

        private static bool TryResolveHostname(IPHostEntry host, [NotNullWhen(true)] out IPAddress? ip)
        {
            if (host.AddressList.Length > 0)
            {
                foreach (IPAddress address in host.AddressList)
                {
                    if (address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        ip = address;
                        return true;
                    }
                }

                ip = null;
                return false;
            }
            else
            {
                ip = null;
                return false;
            }
        }

        private static async Task<IPAddress?> ResolveHostnameAsync(string hostname, bool isPublic)
        {
            try
            {
                string name = hostname;
                if (string.IsNullOrWhiteSpace(hostname))
                {
                    if (isPublic)
                    {
                        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.IP);
                        socket.Connect("8.8.8.8", 65530);
                        if (socket.LocalEndPoint is not IPEndPoint endPoint)
                            return null;

                        return endPoint.Address;
                    }
                    else
                    {
                        name = Dns.GetHostName();
                    }
                }

                var host = await Dns.GetHostEntryAsync(name);
                TryResolveHostname(host, out IPAddress? ip);
                return ip;
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            return Name;
        }

        #endregion
    }
}
