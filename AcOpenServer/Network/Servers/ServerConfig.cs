﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AcOpenServer.Network.Servers
{
    [JsonSourceGenerationOptions(
        WriteIndented = true,
        GenerationMode = JsonSourceGenerationMode.Metadata,
        IncludeFields = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        UseStringEnumConverter = true,
        AllowTrailingCommas = true)]
    [JsonSerializable(typeof(ServerConfig))]
    public partial class ServerConfigSerializerContext : JsonSerializerContext
    {
        
    }

    public class ServerConfig
    {
        public GameType GameType { get; set; }
        public string PublicHostname { get; set; }
        public string PrivateHostname { get; set; }
        public bool Local { get; set; }
        public string PrivateKeyPath { get; set; }
        public string PublicKeyPath { get; set; }
        public int LoginPort { get; set; }
        public int AuthPort { get; set; }
        public int GamePort { get; set; }
        public bool LogInfo { get; set; }
        public bool LogWarnings { get; set; }
        public double LoginClientTimeout { get; set; }
        public double AuthClientTimeout { get; set; }

        public ServerConfig()
        {
            GameType = GameType.ArmoredCoreVerdictDay;
            PublicHostname = "localhost";
            PrivateHostname = "localhost";
            Local = true;
            PrivateKeyPath = string.Empty;
            PublicKeyPath = string.Empty;
            LoginPort = 50011;
            AuthPort = 50008;
            GamePort = 50030;
            LogInfo = true;
            LogWarnings = true;
            LoginClientTimeout = 30d;
            AuthClientTimeout = 30d;
        }

        public static bool Load(string path, [NotNullWhen(true)] out ServerConfig? config)
        {
            if (!File.Exists(path))
            {
                config = null;
                return false;
            }

            try
            {
                config = JsonSerializer.Deserialize(File.ReadAllText(path),
                    ServerConfigSerializerContext.Default.ServerConfig);
                if (config == null)
                {
                    return false;
                }
            }
            catch (Exception)
            {
                config = null;
                return false;
            }

            return true;
        }

        public void Save(string path)
        {
            string json = JsonSerializer.Serialize(this, ServerConfigSerializerContext.Default.ServerConfig);
            File.WriteAllText(path, json);
        }
    }
}
