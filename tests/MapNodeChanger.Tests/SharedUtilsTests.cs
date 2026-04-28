using System.Text.Json.Serialization;
using BaseLib.Config;
using Sls2Mods.Utils.CardText;
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
    public void KeywordBlacklistStateNormalizesAndTracksKeywords()
    {
        var state = new KeywordBlacklistState();

        state.Initialize(
            new[] { " Swift ", "adroit", "swift", "" },
            new[] { "SWIFT" });

        Assert.Equal(new[] { "adroit", "swift" }, state.Keywords);
        Assert.True(state.IsBlacklisted("swift"));
        Assert.False(state.IsBlacklisted("adroit"));

        state.SetBlacklisted("adroit", true);

        Assert.Equal(new[] { "adroit", "swift" }, state.ToBlacklist());
    }

    [Theory]
    [InlineData("zh", "是否开启")]
    [InlineData("zh-Hans", "是否开启")]
    [InlineData("zh-Hant", "是否开启")]
    [InlineData("en", "Enabled")]
    [InlineData("fr-FR", "Enabled")]
    [InlineData("", "Enabled")]
    public void ModMenuLocalizationSelectsTextFromLocale(string locale, string expected)
    {
        Assert.Equal(expected, ModMenuLocalization.EnabledLabel(locale));
    }

    [Fact]
    public void ModConfigLoaderCanSaveConfigExplicitly()
    {
        using var tempRoot = TempConfigRoot.Create();

        ModConfigLoader.Save(
            tempRoot.Path,
            "SavedConfig.json",
            new ExampleConfig { SchemaVersion = 3, Value = 8 });

        var saved = File.ReadAllText(Path.Combine(tempRoot.Path, "SavedConfig.json"));

        Assert.Contains("\"schema_version\"", saved);
        Assert.Contains("\"value\"", saved);
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

    [Fact]
    public void CardTextRewriteAppendsSuffixOnlyOnce()
    {
        Assert.Equal("Strike\u86c7\u54ac", CardTextRewrite.AppendSuffixOnce("Strike", "\u86c7\u54ac"));
        Assert.Equal("Strike\u86c7\u54ac", CardTextRewrite.AppendSuffixOnce("Strike\u86c7\u54ac", "\u86c7\u54ac"));
    }

    [Fact]
    public void CardTextRewriteReplacesChineseDamageText()
    {
        var rewritten = CardTextRewrite.RewriteDamageAsPoison(
            "\u9020\u6210 6 \u70b9\u4f24\u5bb3\u3002",
            "\u86c7\u54ac\uff1a\u653b\u51fb\u4f24\u5bb3\u6539\u4e3a\u7ed9\u4e88\u7b49\u91cf\u4e2d\u6bd2\u3002");

        Assert.Equal("\u7ed9\u4e88 6 \u5c42\u4e2d\u6bd2\u3002", rewritten);
    }

    [Fact]
    public void CardTextRewriteReplacesEnglishDamageText()
    {
        var rewritten = CardTextRewrite.RewriteDamageAsPoison(
            "Deal 6 damage.",
            "\u86c7\u54ac\uff1a\u653b\u51fb\u4f24\u5bb3\u6539\u4e3a\u7ed9\u4e88\u7b49\u91cf\u4e2d\u6bd2\u3002");

        Assert.Equal("Apply 6 Poison.", rewritten);
    }

    [Fact]
    public void CardTextRewriteAppendsFallbackHintForUnrecognizedDamageText()
    {
        var hint = "\u86c7\u54ac\uff1a\u653b\u51fb\u4f24\u5bb3\u6539\u4e3a\u7ed9\u4e88\u7b49\u91cf\u4e2d\u6bd2\u3002";

        var rewritten = CardTextRewrite.RewriteDamageAsPoison("\u62bd 1 \u5f20\u724c\u3002", hint);

        Assert.Equal("\u62bd 1 \u5f20\u724c\u3002\n" + hint, rewritten);
    }

    [Fact]
    public void CardTextRewriteDoesNotAppendFallbackHintTwice()
    {
        var hint = "\u86c7\u54ac\uff1a\u653b\u51fb\u4f24\u5bb3\u6539\u4e3a\u7ed9\u4e88\u7b49\u91cf\u4e2d\u6bd2\u3002";
        var description = "\u62bd 1 \u5f20\u724c\u3002\n" + hint;

        var rewritten = CardTextRewrite.RewriteDamageAsPoison(description, hint);

        Assert.Equal(description, rewritten);
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
