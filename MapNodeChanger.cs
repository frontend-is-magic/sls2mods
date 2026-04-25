using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Runs;

namespace MapNodeChanger;

[ModInitializer("ModLoaded")]
public static class MapNodeChanger
{
    private const string ModId = "MapNodeChanger";
    private const int SupportedSchemaVersion = 1;

    private static RunState? _runState;
    private static Config _config = Config.Default();
    private static readonly HashSet<int> ModifiedActs = new();

    public static void ModLoaded()
    {
        LoadConfig();

        var manager = RunManager.Instance;
        manager.RunStarted += OnRunStarted;
        manager.ActEntered += OnActEntered;

        LogInfo("loaded");
    }

    private static void OnRunStarted(RunState runState)
    {
        _runState = runState;
        ModifiedActs.Clear();
        TryChangeCurrentActMap();
    }

    private static void OnActEntered()
    {
        TryChangeCurrentActMap();
    }

    private static void TryChangeCurrentActMap()
    {
        if (!_config.Enabled)
        {
            LogInfo("skipped because config is disabled");
            return;
        }

        var runState = _runState;
        if (runState?.Map?.StartingMapPoint == null)
        {
            LogInfo("skipped because map is not ready");
            return;
        }

        if (!ModifiedActs.Add(runState.CurrentActIndex))
        {
            LogInfo($"act {runState.CurrentActIndex} already modified");
            return;
        }

        var points = CollectMapPoints(runState.Map.StartingMapPoint);
        var rng = CreateRandom(runState.CurrentActIndex);
        var totalChanges = 0;

        foreach (var rule in _config.Rules.Where(static rule => rule.Enabled))
        {
            totalChanges += ApplyRule(rule, points, runState.CurrentMapPoint, rng);
        }

        LogInfo($"modified act {runState.CurrentActIndex}: {totalChanges} node(s) changed across {points.Count} visited node(s)");
    }

    private static int ApplyRule(Rule rule, IReadOnlyList<MapPoint> points, MapPoint? currentPoint, Random rng)
    {
        if (!TryParsePointType(rule.From, out var from) || !TryParsePointType(rule.To, out var to))
        {
            LogInfo($"rule '{rule.Name}' skipped because point type is invalid: {rule.From} -> {rule.To}");
            return 0;
        }

        if (!IsMutableTargetType(from) || !IsMutableTargetType(to))
        {
            LogInfo($"rule '{rule.Name}' skipped because protected point type is used: {from} -> {to}");
            return 0;
        }

        var changed = 0;
        foreach (var point in points.OrderBy(static point => point.coord.ToString()))
        {
            if (rule.MaxChanges > 0 && changed >= rule.MaxChanges)
            {
                break;
            }

            if (_config.SkipCurrentNode && ReferenceEquals(point, currentPoint))
            {
                continue;
            }

            if (point.PointType != from || rng.NextDouble() > Clamp01(rule.Chance))
            {
                continue;
            }

            point.PointType = to;
            changed++;
            LogInfo($"rule '{rule.Name}' changed {point.coord}: {from} -> {to}");
        }

        return changed;
    }

    private static List<MapPoint> CollectMapPoints(MapPoint start)
    {
        var points = new List<MapPoint>();
        var visited = new HashSet<MapPoint>();
        var stack = new Stack<MapPoint>();
        stack.Push(start);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!visited.Add(current))
            {
                continue;
            }

            points.Add(current);

            if (current.Children == null)
            {
                continue;
            }

            foreach (var child in current.Children)
            {
                if (child != null)
                {
                    stack.Push(child);
                }
            }
        }

        return points;
    }

    private static bool IsMutableTargetType(MapPointType type)
    {
        return type is MapPointType.Unknown
            or MapPointType.Monster
            or MapPointType.Elite
            or MapPointType.RestSite
            or MapPointType.Treasure
            or MapPointType.Shop;
    }

    private static Random CreateRandom(int actIndex)
    {
        if (_config.Seed == 0)
        {
            return new Random();
        }

        return new Random(HashCode.Combine(_config.Seed, actIndex));
    }

    private static void LoadConfig()
    {
        var path = GetConfigPath();
        if (!File.Exists(path))
        {
            _config = Config.Default();
            SaveConfig(path, _config);
            LogInfo($"created default config at {path}");
            return;
        }

        try
        {
            var json = File.ReadAllText(path);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var loaded = JsonSerializer.Deserialize<Config>(json, options);
            if (loaded == null || loaded.SchemaVersion != SupportedSchemaVersion)
            {
                _config = Config.Default();
                LogInfo($"config schema is unsupported, using defaults: {loaded?.SchemaVersion}");
                return;
            }

            _config = loaded.WithDefaults();
            LogInfo($"loaded config from {path}");
        }
        catch (Exception ex)
        {
            _config = Config.Default();
            LogInfo($"failed to load config, using defaults: {ex.Message}");
        }
    }

    private static string GetConfigPath()
    {
        return Path.Combine(AppContext.BaseDirectory, "MapNodeChangerConfig.json");
    }

    private static void SaveConfig(string path, Config config)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            File.WriteAllText(path, JsonSerializer.Serialize(config, options));
        }
        catch (Exception ex)
        {
            LogInfo($"failed to write default config: {ex.Message}");
        }
    }

    private static bool TryParsePointType(string? value, out MapPointType type)
    {
        return Enum.TryParse(value, ignoreCase: true, out type);
    }

    private static double Clamp01(double value)
    {
        if (double.IsNaN(value))
        {
            return 0;
        }

        return Math.Clamp(value, 0, 1);
    }

    private static void LogInfo(string message)
    {
        Log.Warn($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {ModId}: {message}");
    }

    private sealed class Config
    {
        [JsonPropertyName("schema_version")]
        public int SchemaVersion { get; set; } = SupportedSchemaVersion;

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonPropertyName("seed")]
        public int Seed { get; set; }

        [JsonPropertyName("skip_current_node")]
        public bool SkipCurrentNode { get; set; } = true;

        [JsonPropertyName("rules")]
        public List<Rule> Rules { get; set; } = new();

        public static Config Default()
        {
            return new Config
            {
                Rules =
                {
                    new Rule
                    {
                        Name = "Unknown to Shop",
                        From = "Unknown",
                        To = "Shop",
                        Chance = 0.15,
                        MaxChanges = 2
                    },
                    new Rule
                    {
                        Name = "Unknown to RestSite",
                        From = "Unknown",
                        To = "RestSite",
                        Chance = 0.10,
                        MaxChanges = 2
                    },
                    new Rule
                    {
                        Name = "Unknown to Elite",
                        Enabled = false,
                        From = "Unknown",
                        To = "Elite",
                        Chance = 0.05,
                        MaxChanges = 1
                    }
                }
            };
        }

        public Config WithDefaults()
        {
            Rules ??= new List<Rule>();
            return this;
        }
    }

    private sealed class Rule
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "Unnamed rule";

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonPropertyName("from")]
        public string From { get; set; } = "Unknown";

        [JsonPropertyName("to")]
        public string To { get; set; } = "Shop";

        [JsonPropertyName("chance")]
        public double Chance { get; set; } = 0.1;

        [JsonPropertyName("max_changes")]
        public int MaxChanges { get; set; } = 1;
    }
}
