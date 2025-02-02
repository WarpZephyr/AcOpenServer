using AcOpenServer.Core.Crypto;
using AcOpenServer.Core.Logging;
using AcOpenServer.Network.Servers;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace AcOpenServer.Network
{
    public class ServerManager
    {
        private const string DefaultServerName = "Default";
        private const string PublicKeyFileName = "publickey.pem";
        private const string PrivateKeyFileName = "privatekey.pem";
        private const string ServerConfigFileName = "config.json";

        private readonly Logger Log;
        private readonly string ServersFolder;
        private readonly List<Server> Servers;
        private bool QuitServers;

        public ServerManager(string serversFolder, Logger log)
        {
            Log = log;
            ServersFolder = serversFolder;
            Servers = [];
            
            bool serverFound = false;
            foreach (var folder in Directory.EnumerateDirectories(serversFolder))
            {
                Servers.Add(new Server(folder, new Logger()));
                serverFound = true;
            }

            if (!serverFound)
            {
                Log.Info("No servers found, making default server.");
                Servers.Add(MakeDefaultServer(serversFolder));
            }
        }

        private Server MakeDefaultServer(string serversFolder)
        {
            string serverFolder = Path.Combine(serversFolder, DefaultServerName);
            Directory.CreateDirectory(serverFolder);

            string publicPath = Path.Combine(serverFolder, PublicKeyFileName);
            string privatePath = Path.Combine(serverFolder, PrivateKeyFileName);
            string serverConfigPath = Path.Combine(serverFolder, ServerConfigFileName);

            var key = RSAKey.Generate();
            key.Save(privatePath, publicPath);

            var config = new ServerConfig();
            config.Save(serverConfigPath);

            return new Server(serverFolder, DefaultServerName, key, config, Log);
        }

        private async Task StartServersAsync()
        {
            Log.Info("Starting servers...");
            for (int i = Servers.Count - 1; i >= 0; i--)
            {
                var server = Servers[i];
                if (!await server.StartAsync())
                {
                    Log.Error($"Failed to start server: {server.Name}");
                    Servers.RemoveAt(i);
                }
                else
                {
                    Log.Info($"Started server: {server.Name}");
                }
            }
        }

        private void EndServers()
        {
            Log.Info("Shutting servers down...");
            foreach (var server in Servers)
            {
                if (!server.End())
                {
                    Log.Error($"Failed to shutdown server: {server.Name}");
                }
                else
                {
                    Log.Info($"Shutdown server: {server.Name}");
                }
            }
        }

        public async Task RunAsync()
        {
            Log.Info($"Starting run...");
            await StartServersAsync();
            if (Servers.Count == 0)
            {
                Log.Warning("No servers were started, quitting.");
                return;
            }

            while (!QuitServers)
            {
                if (Servers.Count == 0)
                {
                    Log.Info($"No servers are running, quitting.");
                    return;
                }

                foreach (Server server in Servers)
                {
                    await server.UpdateAsync();
                }
            }

            EndServers();
        }

        public void Quit()
        {
            QuitServers = true;
        }
    }
}
