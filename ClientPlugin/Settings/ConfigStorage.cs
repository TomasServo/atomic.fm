using System;
using System.IO;
using System.Xml.Serialization;
using VRage.FileSystem;
using VRage.Utils;

namespace ClientPlugin.Settings
{
    public static class ConfigStorage
    {
        private static readonly string ConfigFileName = string.Concat(Plugin.Name, ".cfg");
        private static string ConfigFilePath => Path.Combine(MyFileSystem.UserDataPath, "Storage", ConfigFileName);

        public static void Save(Config config)
        {
            var path = ConfigFilePath;
            using (var text = File.CreateText(path))
                new XmlSerializer(typeof(Config)).Serialize(text, config);
        }

        public static Config Load()
        {
            var path = ConfigFilePath;
            if (!File.Exists(path))
            {
                return Config.Default;
            }

            var xmlSerializer = new XmlSerializer(typeof(Config));
            try
            {
                using (var streamReader = File.OpenText(path))
                {
                    var config = (Config)xmlSerializer.Deserialize(streamReader) ?? Config.Default;
                    Normalize(config);
                    return config;
                }
            }
            catch (Exception)
            {
                MyLog.Default.Warning($"{ConfigFileName}: Failed to read config file: {ConfigFilePath}");
            }
            
            return Config.Default;
        }

        private static void Normalize(Config config)
        {
            if (config == null)
                return;

            bool oldConfig = config.ConfigVersion < Config.CurrentConfigVersion;

            if (string.IsNullOrWhiteSpace(config.StreamUrl))
                config.StreamUrl = Config.DefaultStreamUrl;

            if (config.Volume <= 0f || oldConfig)
                config.Volume = Config.DefaultVolume;

            config.ConfigVersion = Config.CurrentConfigVersion;
        }
        
    }
}
