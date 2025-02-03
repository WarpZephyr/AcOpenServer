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
        private readonly List<Server> Servers;
        private readonly List<Task> ServerTasks;

        public ServerManager(string serversFolder, Logger log)
        {
            Log = log;
            Servers = [];
            ServerTasks = [];

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

        public Task StartServersAsync()
        {
            Log.Info("Starting servers...");
            foreach (var server in Servers)
            {
                ServerTasks.Add(server.StartAsync().ContinueWith(ServerCleanup));
                Log.Info($"Started {server}");
            }

            return Task.WhenAll(ServerTasks);
        }

        #region Cleanup

        private void ServerCleanup(Task task)
        {
            if (task.Exception != null)
            {
                Log.Error($"Client disconnected due to an error: {task.Exception}");
            }

            ServerTasks.Remove(task);
        }

        #endregion
    }
}
