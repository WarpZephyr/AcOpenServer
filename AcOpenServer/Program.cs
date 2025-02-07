using AcOpenServer.Logging;
using AcOpenServer.Network;
using AcOpenServer.Tests;
using AcOpenServer.Utilities;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace AcOpenServer
{
    internal class Program
    {
        static readonly string ProgramFolder = GetProgramFolder();
        static readonly string ServersFolder = Path.Combine(ProgramFolder, "Saved");
        static readonly Logger Log = StartLog();

        static async Task Main(string[] args)
        {
            
#if DEBUG
            await HandleTests(args);
#endif
            await HandleServer();
        }

        #region Server

        static async Task HandleServer()
        {
            Directory.CreateDirectory(ServersFolder);
            var serverManager = new ServerManager(ServersFolder, Log);
            await serverManager.StartServersAsync();
        }

        #endregion

#if DEBUG

        #region Tests

        static async Task HandleTests(string[] args)
        {
            bool runRsaTest = false;
            bool runCwcTest = false;
            bool runTcpTest = false;
            string publicKeyPath = string.Empty;
            string privateKeyPath = string.Empty;

            bool scanPublicKeyPathNext = false;
            bool scanPrivateKeyPathNext = false;
            foreach (string arg in args)
            {
                if (scanPublicKeyPathNext)
                {
                    publicKeyPath = arg;
                    scanPublicKeyPathNext = false;
                    continue;
                }

                if (scanPrivateKeyPathNext)
                {
                    privateKeyPath = arg;
                    scanPrivateKeyPathNext = false;
                    continue;
                }

                switch (arg)
                {
                    case "-rsatest":
                        runRsaTest = true;
                        break;
                    case "-cwctest":
                        runCwcTest = true;
                        break;
                    case "-tcptest":
                        runTcpTest = true;
                        break;
                    case "-publickey":
                        scanPublicKeyPathNext = true;
                        break;
                    case "-privatekey":
                        scanPrivateKeyPathNext = true;
                        break;
                }
            }

            if (string.IsNullOrWhiteSpace(publicKeyPath))
            {
                publicKeyPath = GetPublicKeyPath();
            }

            if (string.IsNullOrWhiteSpace(privateKeyPath))
            {
                privateKeyPath = GetPrivateKeyPath();
            }

            if (runRsaTest)
            {
                Console.WriteLine("Running RSA test...");
                RunRsaTest(args, publicKeyPath, privateKeyPath);
            }

            if (runCwcTest)
            {
                Console.WriteLine("Running CWC test...");
                RunCwcTest(args);
            }

            if (runTcpTest)
            {
                Console.WriteLine("Running TCP test...");
                var loginTask = RunTcpTestAsync(50011, null, "Login");
                await loginTask;
            }
        }

        static void RunRsaTest(string[] args, string publicKeyPath, string privateKeyPath)
        {
            bool doDecrypt = false;
            bool doEncrypt = false;
            bool doPublic = false;
            bool doPrivate = false;
            string inputPath = string.Empty;
            string outputPath = string.Empty;
            bool doHexDump = false;

            bool skipNext = false;
            bool scanInputPathNext = false;
            bool scanOutputPathNext = false;
            foreach (string arg in args)
            {
                if (skipNext)
                {
                    skipNext = false;
                    continue;
                }

                if (scanInputPathNext)
                {
                    inputPath = arg;
                    scanInputPathNext = false;
                    continue;
                }

                if (scanOutputPathNext)
                {
                    outputPath = arg;
                    scanOutputPathNext = false;
                    continue;
                }

                switch (arg)
                {
                    case "-rsatest":
                        continue;
                    case "-public":
                        if (doPrivate)
                        {
                            throw new Exception("Already specified private mode.");
                        }

                        doPublic = true;
                        doPrivate = false;
                        break;
                    case "-private":
                        if (doPublic)
                        {
                            throw new Exception("Already specified public mode.");
                        }

                        doPrivate = true;
                        doPublic = false;
                        break;
                    case "-decrypt":
                        if (doEncrypt)
                        {
                            throw new Exception("Already specified encryption.");
                        }

                        doDecrypt = true;
                        doEncrypt = false;
                        break;
                    case "-encrypt":
                        if (doDecrypt)
                        {
                            throw new Exception("Already specified decryption.");
                        }

                        doEncrypt = true;
                        break;
                    case "-input":
                        scanInputPathNext = true;
                        break;
                    case "-output":
                        scanOutputPathNext = true;
                        break;
                    case "-hexdump":
                        doHexDump = true;
                        break;
                    case "-publickey":
                    case "-privatekey":
                        skipNext = true;
                        continue;
                }
            }

            if (string.IsNullOrWhiteSpace(inputPath))
            {
                throw new Exception("Input data path not specified.");
            }

            string keyPath = doPublic ? publicKeyPath : privateKeyPath;
            if (!File.Exists(keyPath))
            {
                throw new Exception("Key could not be found.");
            }
            
            string keyStr = File.ReadAllText(keyPath);
            var test = new RsaTest(keyStr, doPublic || !doPrivate, doEncrypt || !doDecrypt);

            byte[] input = File.ReadAllBytes(inputPath);
            byte[] output = test.Run(input);
            if (!string.IsNullOrWhiteSpace(outputPath))
            {
                string? outputFolder = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrWhiteSpace(outputFolder))
                {
                    Directory.CreateDirectory(outputFolder);
                }

                File.WriteAllBytes(outputPath, output);
            }

            if (doHexDump)
            {
                Console.WriteLine(output.ToHexView(0x10));
            }
        }

        static void RunCwcTest(string[] args)
        {
            bool doDecrypt = false;
            bool doEncrypt = false;
            string keyPath = string.Empty;
            string inputPath = string.Empty;
            string outputPath = string.Empty;
            bool doHexDump = false;

            bool scanInputPathNext = false;
            bool scanOutputPathNext = false;
            bool scanKeyPathNext = false;
            foreach (string arg in args)
            {
                if (scanKeyPathNext)
                {
                    keyPath = arg;
                    scanKeyPathNext = false;
                    continue;
                }

                if (scanInputPathNext)
                {
                    inputPath = arg;
                    scanInputPathNext = false;
                    continue;
                }

                if (scanOutputPathNext)
                {
                    outputPath = arg;
                    scanOutputPathNext = false;
                    continue;
                }

                switch (arg)
                {
                    case "-cwctest":
                        continue;
                    case "-decrypt":
                        if (doEncrypt)
                        {
                            throw new Exception("Already specified encryption.");
                        }

                        doDecrypt = true;
                        doEncrypt = false;
                        break;
                    case "-encrypt":
                        if (doDecrypt)
                        {
                            throw new Exception("Already specified decryption.");
                        }

                        doEncrypt = true;
                        break;
                    case "-key":
                        scanKeyPathNext = true;
                        continue;
                    case "-input":
                        scanInputPathNext = true;
                        break;
                    case "-output":
                        scanOutputPathNext = true;
                        break;
                    case "-hexdump":
                        doHexDump = true;
                        break;
                }
            }

            if (string.IsNullOrWhiteSpace(inputPath))
            {
                throw new Exception("Input data path not specified.");
            }

            if (!File.Exists(keyPath))
            {
                throw new Exception("Key could not be found.");
            }

            byte[] key = File.ReadAllBytes(keyPath);
            var test = new CwcTest(key, doEncrypt || !doDecrypt);

            byte[] input = File.ReadAllBytes(inputPath);
            byte[] output = test.Run(input);
            if (!string.IsNullOrWhiteSpace(outputPath))
            {
                string? outputFolder = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrWhiteSpace(outputFolder))
                {
                    Directory.CreateDirectory(outputFolder);
                }

                File.WriteAllBytes(outputPath, output);
            }

            if (doHexDump)
            {
                Console.WriteLine(output.ToHexView(0x10));
            }
        }

        static async Task RunTcpTestAsync(int port, byte[]? reply, string taskName)
        {
            IPAddress ip = new IPAddress([127, 0, 0, 1]);
            IPEndPoint ipEndPoint = new(ip, port);
            using Socket listener = new(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(ipEndPoint);
            listener.Listen(100);

            string ipString = $"{ip}:{port}";
            Console.WriteLine($"{taskName} task listening on {ipString}...");
            var handler = await listener.AcceptAsync();
            while (true)
            {
                // Receive message.
                var buffer = new byte[1024];
                int received = await handler.ReceiveAsync(buffer, SocketFlags.None);
                if (received > 0)
                {
                    Console.WriteLine($"{taskName} task received message: \n{buffer.ToHexView(0x10, received)}");
                    if (reply != null)
                    {
                        Console.WriteLine($"{taskName} task is replying...");
                        await handler.SendAsync(reply, 0);
                        Console.WriteLine($"{taskName} task has replied.");
                    }
                    break;
                }
            }
        }

        #endregion

#endif

        #region Util

        static string GetProgramFolder()
        {
            string path = AppDomain.CurrentDomain.BaseDirectory;
            string? folder = Path.GetDirectoryName(path);
            if (string.IsNullOrWhiteSpace(folder))
            {
                throw new Exception($"Could not determine program folder.");
            }

            return folder;
        }

        static string GetPublicKeyPath()
        {
            return Path.Combine(ProgramFolder, "publickey.pem");
        }

        static string GetPrivateKeyPath()
        {
            return Path.Combine(ProgramFolder, "privatekey.pem");
        }

        static Logger StartLog()
        {
            return new Logger();
        }

        #endregion
    }
}
