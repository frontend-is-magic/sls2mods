using System.Text.Json.Serialization;
using BaseLib.Config;
using Sls2Mods.Utils.Config;
using Sls2Mods.Utils.Randoming;
using Xunit;

namespace MapNodeChanger.Tests;

public sealed class SharedUtilsTests
{
    [Fact]
    public void DeterministicSeedReturnsStableValue()
    {
        var first = DeterministicSeed.FromString("CardRewardEnchantments|run=123|reward=2|card=0");
        var second = DeterministicSeed.FromString("CardRewardEnchantments|run=123|reward=2|card=0");

        Assert.Equal(first, second);
        Assert.NotEqual(0u, first);
    }

    [Fact]
    public void DeterministicSeedChangesForDifferentInput()
    {
        var first = DeterministicSeed.FromString("card=0");
        var second = DeterministicSeed.FromString("card=1");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void ModConfigLoaderCreatesDefaultConfigWhenMissing()
    {
        using var tempRoot = TempConfigRoot.Create();
        var logs = new List<string>();

        var config = ModConfigLoader.LoadOrCreate(
            tempRoot.Path,
            "ExampleMod",
            "ExampleConfig.json",
            supportedSchemaVersion: 3,
            createDefault: () => new ExampleConfig { Value = 7 },
            normalize: item =>
            {
                item.Value = Math.Clamp(item.Value, 0, 10);
                return item;
            },
            log: logs.Add);

        Assert.Equal(3, config.SchemaVersion);
        Assert.Equal(7, config.Value);
        Assert.True(File.Exists(Path.Combine(tempRoot.Path, "ExampleConfig.json")));
        Assert.Contains(logs, message => message.Contains("created default config"));
    }

    [Fact]
    public void ModConfigLoaderFallsBackForUnsupportedSchema()
    {
        using var tempRoot = TempConfigRoot.Create();
        File.WriteAllText(Path.Combine(tempRoot.Path, "ExampleConfig.json"), """{"schema_version":2,"value":99}""");

        var config = ModConfigLoader.LoadOrCreate(
            tempRoot.Path,
            "ExampleMod",
            "ExampleConfig.json",
            supportedSchemaVersion: 3,
            createDefault: () => new ExampleConfig { Value = 5 },
            normalize: item => item,
            log: _ => { });

        Assert.Equal(3, config.SchemaVersion);
        Assert.Equal(5, config.Value);
    }

    [Fact]
    public void ModConfigLoaderFallsBackForBadJson()
    {
        using var tempRoot = TempConfigRoot.Create();
        File.WriteAllText(Path.Combine(tempRoot.Path, "ExampleConfig.json"), "{bad json");

        var config = ModConfigLoader.LoadOrCreate(
            tempRoot.Path,
            "ExampleMod",
            "ExampleConfig.json",
            supportedSchemaVersion: 3,
            createDefault: () => new ExampleConfig { Value = 4 },
            normalize: item => item,
            log: _ => { });

        Assert.Equal(3, config.SchemaVersion);
        Assert.Equal(4, config.Value);
    }

    private sealed class ExampleConfig : IModConfig
    {
        [JsonPropertyName("schema_version")]
        public int SchemaVersion { get; set; } = 3;

        [JsonPropertyName("value")]
        public int Value { get; set; }
    }

    private sealed class TempConfigRoot : IDisposable
    {
        private TempConfigRoot(string path)
        {
            Path = path;
            Directory.CreateDirectory(path);
        }

        public string Path { get; }

        public static TempConfigRoot Create()
        {
            return new TempConfigRoot(System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"MapNodeChangerTests-{Guid.NewGuid():N}"));
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
