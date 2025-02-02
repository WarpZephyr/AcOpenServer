using AcOpenServer.Core.Crypto;
using AcOpenServer.Core.Logging;
using AcOpenServer.Network.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace AcOpenServer.Network.Servers
{
    public class Server
    {
        private const string ConfigFileName = "config.json";
        private const string PublicKeyFileName = "publickey.pem";
        private const string PrivateKeyFileName = "privatekey.pem";

        private readonly Logger Log;
        private readonly string ServerFolder;
        private readonly List<IService> Services;
        private ServerConfig? Config;
        private RSAKey? PrivateKey;
        private IPAddress? PublicIP;
        private IPAddress? PrivateIP;

        public string Name { get; init; }

        public Server(string serverFolder, Logger log)
        {
            Log = log;
            ServerFolder = serverFolder;
            Services = [];

            Name = Path.GetFileName(serverFolder);
        }

        public Server(string serverFolder, string name, RSAKey privateKey, ServerConfig config, Logger log)
        {
            Log = log;
            ServerFolder = serverFolder;
            PrivateKey = privateKey;
            Config = config;
            Services = [];

            Name = name;
        }

        public async Task<bool> StartAsync()
        {
            if (!Directory.Exists(ServerFolder))
            {
                Log.Error($"Server folder doesn't exist.");
                return false;
            }

            if (Config == null)
            {
                string configPath = Path.Combine(ServerFolder, ConfigFileName);
                if (!File.Exists(configPath))
                {
                    Log.Warning($"Could not find {ConfigFileName} for {Name} server, making a default config.");
                    Config = new ServerConfig();
                    Config.Save(configPath);
                }
                else if (!ServerConfig.Load(configPath, out ServerConfig? config))
                {
                    Log.Warning($"Failed to load {ConfigFileName} for {Name} server, using default config.");
                    Config = new ServerConfig();
                }
                else
                {
                    Config = config;
                }
            }

#if DEBUG
            Log.EnabledChannels = Logger.LogChannelFlags.Debug;
#else
            Log.EnabledChannels = Logger.LogChannelFlags.None;
#endif
            if (Config.LogInfo)
            {
                Log.EnabledChannels |= Logger.LogChannelFlags.Info;
            }

            if (Config.LogWarnings)
            {
                Log.EnabledChannels |= Logger.LogChannelFlags.Warning;
            }

            if (Config.LogErrors)
            {
                Log.EnabledChannels |= Logger.LogChannelFlags.Error;
            }

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
                try
                {
                    var publicHost = await Dns.GetHostEntryAsync(Config.PublicHostname);
                    if (publicHost.AddressList.Length > 0)
                    {
                        bool foundIPV4 = false;
                        foreach (var address in publicHost.AddressList)
                        {
                            if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            {
                                PublicIP = address;
                                foundIPV4 = true;
                                break;
                            }
                        }

                        if (!foundIPV4)
                        {
                            Log.Error($"Failed to find a usable IPV4 address from the public host: {Config.PublicHostname}");
                            return false;
                        }
                    }
                    else
                    {
                        Log.Error($"Failed to resolve public hostname: {Config.PublicHostname}");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"An error occurred while resolving public hostname: {Config.PublicHostname}\n{ex}");
                    return false;
                }

                try
                {
                    var privateHost = await Dns.GetHostEntryAsync(Config.PrivateHostname);
                    if (privateHost.AddressList.Length > 0)
                    {
                        bool foundIPV4 = false;
                        foreach (var address in privateHost.AddressList)
                        {
                            if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            {
                                PrivateIP = address;
                                foundIPV4 = true;
                                break;
                            }
                        }

                        if (!foundIPV4)
                        {
                            Log.Error($"Failed to find a usable IPV4 address from the private host: {Config.PrivateHostname}");
                            return false;
                        }
                    }
                    else
                    {
                        Log.Error($"Failed to resolve private hostname: {Config.PrivateHostname}");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"An error occurred while resolving private hostname: {Config.PrivateHostname}\n{ex}");
                    return false;
                }
            }

            Log.Info($"Public ip: {PublicIP}");
            Log.Info($"Private ip: {PrivateIP}");

            var serverIP = new IPAddress([0, 0, 0, 0]);
            var loginService = new LoginService(Name, serverIP, PrivateKey, Config.LoginPort, Config.AuthPort, Config.ClientTimeout, Log);
            var authService = new AuthService(Name, serverIP, PrivateKey, Config.AuthPort, Config.ClientTimeout, Log);

            Services.Add(loginService);
            Services.Add(authService);

            StartServices();
            return true;
        }

        public bool End()
        {
            return true;
        }

        public async Task UpdateAsync()
        {
            foreach (var service in Services)
            {
                await service.UpdateAsync();
            }
        }

        private void StartServices()
        {
            foreach (var service in Services)
            {
                service.Start();
            }
        }

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
    }
}
