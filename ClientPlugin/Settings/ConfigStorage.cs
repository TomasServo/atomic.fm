using System;
using System.IO;
using System.Xml.Serialization;
using VRage.FileSystem;
using VRage.Utils;

namespace ClientPlugin.Settings;

public static class ConfigStorage
{
    private static readonly string ConfigFileName = string.Concat(Plugin.Name, ".cfg");
    private static string ConfigFilePath => Path.Combine(MyFileSystem.UserDataPath, "Storage", ConfigFileName);

    public static void Save(Config config)
    {
        var path = ConfigFilePath;
        Directory.CreateDirectory(Path.GetDirectoryName(path));
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
                NormalizeVolume(config);
                return config;
            }
        }
        catch (Exception)
        {
            MyLog.Default.Warning($"{ConfigFileName}: Failed to read config file: {ConfigFilePath}");
        }
            
        return Config.Default;
    }

    /// <summary>
    /// Clamp volume to 0.0–11.0 and snap to one decimal place (e.g. 1.5).
    /// </summary>
    private static void NormalizeVolume(Config config)
    {
        if (config == null)
            return;

        config.Volume = Config.ClampVolume(config.Volume);
    }
}