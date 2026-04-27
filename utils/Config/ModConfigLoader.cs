using System;
using System.IO;
using System.Text.Json;
using Godot;

namespace Sls2Mods.Utils.Config;

public static class ModConfigLoader
{
    public static string DefaultConfigRoot()
    {
        return Path.Combine(OS.GetUserDataDir(), "mod_configs");
    }

    public static string DefaultConfigPath(string fileName)
    {
        return Path.Combine(DefaultConfigRoot(), fileName);
    }

    public static T LoadOrCreate<T>(
        string configRoot,
        string modId,
        string fileName,
        int supportedSchemaVersion,
        Func<T> createDefault,
        Func<T, T> normalize,
        Action<string> log)
        where T : IModConfig
    {
        var path = Path.Combine(configRoot, fileName);
        if (!File.Exists(path))
        {
            var created = createDefault();
            created.SchemaVersion = supportedSchemaVersion;
            Save(path, created);
            log($"{modId}: created default config at {path}");
            return normalize(created);
        }

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var loaded = JsonSerializer.Deserialize<T>(File.ReadAllText(path), options);
            if (loaded == null || loaded.SchemaVersion != supportedSchemaVersion)
            {
                log($"{modId}: config schema is unsupported, using defaults: {loaded?.SchemaVersion}");
                var fallback = createDefault();
                fallback.SchemaVersion = supportedSchemaVersion;
                return normalize(fallback);
            }

            return normalize(loaded);
        }
        catch (Exception ex)
        {
            log($"{modId}: failed to load config, using defaults: {ex.Message}");
            var fallback = createDefault();
            fallback.SchemaVersion = supportedSchemaVersion;
            return normalize(fallback);
        }
    }

    public static T LoadOrCreate<T>(
        string modId,
        string fileName,
        int supportedSchemaVersion,
        Func<T> createDefault,
        Func<T, T> normalize,
        Action<string> log)
        where T : IModConfig
    {
        return LoadOrCreate(DefaultConfigRoot(), modId, fileName, supportedSchemaVersion, createDefault, normalize, log);
    }

    public static void Save<T>(string configRoot, string fileName, T config)
    {
        Save(Path.Combine(configRoot, fileName), config);
    }

    public static void Save<T>(string path, T config)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        File.WriteAllText(path, JsonSerializer.Serialize(config, options));
    }
}
