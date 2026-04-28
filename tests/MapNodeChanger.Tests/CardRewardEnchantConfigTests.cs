using CardRewardEnchantments.Features.CardRewards;
using Xunit;

namespace MapNodeChanger.Tests;

public sealed class CardRewardEnchantConfigTests
{
    public CardRewardEnchantConfigTests()
    {
        ResetMenuDefaults();
    }

    [Fact]
    public void MenuDefaultsConvertToRuntimeConfig()
    {
        var config = CardRewardEnchantConfigMenu.ToRuntimeConfig();

        Assert.True(config.Enabled);
        Assert.Equal(1.0, config.EnchantChance);
        Assert.Empty(config.BlacklistedKeywords);
    }

    [Fact]
    public void MenuChanceClampsToProbability()
    {
        CardRewardEnchantConfigMenu.EnchantChancePercent = 150.0;
        Assert.Equal(1.0, CardRewardEnchantConfigMenu.ToRuntimeConfig().EnchantChance);

        CardRewardEnchantConfigMenu.EnchantChancePercent = -25.0;
        Assert.Equal(0.0, CardRewardEnchantConfigMenu.ToRuntimeConfig().EnchantChance);
    }

    [Fact]
    public void MenuCheckboxesConvertToBlacklist()
    {
        CardRewardEnchantConfigMenu.Enabled = true;
        CardRewardEnchantConfigMenu.EnchantChancePercent = 100.0;
        CardRewardEnchantConfigMenu.LogRolls = true;
        CardRewardEnchantConfigMenu.InitializeFrom(
            new CardRewardEnchantConfig { BlacklistedKeywords = new List<string> { "innate", "ethereal" } },
            new[] { "innate", "retain", "ethereal" });

        var config = CardRewardEnchantConfigMenu.ToRuntimeConfig();

        Assert.Equal(new[] { "ethereal", "innate" }, config.BlacklistedKeywords);
    }

    [Fact]
    public void MenuCanInitializeFromJsonConfig()
    {
        CardRewardEnchantConfigMenu.InitializeFrom(new CardRewardEnchantConfig
        {
            Enabled = false,
            EnchantChance = 0.25,
            BlacklistedKeywords = new List<string> { " Retain ", "ethereal" },
            LogRolls = false
        }, new[] { "adroit", "retain", "ethereal" });

        var config = CardRewardEnchantConfigMenu.ToRuntimeConfig(new CardRewardEnchantConfig { LogRolls = false });

        Assert.False(config.Enabled);
        Assert.Equal(0.25, config.EnchantChance);
        Assert.Equal(new[] { "ethereal", "retain" }, config.BlacklistedKeywords);
        Assert.False(config.LogRolls);
    }

    [Fact]
    public void MenuSourceAddsEnabledOptionBeforeGeneratedOptions()
    {
        var source = File.ReadAllText(FindRepoFile("mods/CardRewardEnchantments/Features/CardRewards/CardRewardEnchantConfigMenu.cs"));

        Assert.True(
            source.IndexOf("AddEnabledOption", StringComparison.Ordinal) <
            source.IndexOf("GenerateOptionsForAllProperties", StringComparison.Ordinal));
        Assert.Contains("ConfigHideInUI", source);
    }

    [Fact]
    public void MenuInitializesDynamicKeywordBlacklist()
    {
        CardRewardEnchantConfigMenu.InitializeFrom(
            new CardRewardEnchantConfig
            {
                BlacklistedKeywords = new List<string> { "swift", "vigorous" }
            },
            new[] { "adroit", "swift", "vigorous" });

        var config = CardRewardEnchantConfigMenu.ToRuntimeConfig();

        Assert.False(CardRewardEnchantConfigMenu.IsKeywordBlacklisted("adroit"));
        Assert.True(CardRewardEnchantConfigMenu.IsKeywordBlacklisted("swift"));
        Assert.True(CardRewardEnchantConfigMenu.IsKeywordBlacklisted("vigorous"));
        Assert.Equal(new[] { "swift", "vigorous" }, config.BlacklistedKeywords);
    }

    [Fact]
    public void RuntimeConfigNormalizeClampsAndDeduplicates()
    {
        var config = new CardRewardEnchantConfig
        {
            EnchantChance = double.NaN,
            BlacklistedKeywords = new List<string> { " Innate ", "", "innate", "retain" }
        };

        config.Normalize();

        Assert.Equal(0.0, config.EnchantChance);
        Assert.Equal(new[] { "innate", "retain" }, config.BlacklistedKeywords);
    }

    [Fact]
    public void RuntimeConfigNormalizeIgnoresNullBlacklistValues()
    {
        var nullListConfig = new CardRewardEnchantConfig
        {
            EnchantChance = 0.5,
            BlacklistedKeywords = null!
        };

        nullListConfig.Normalize();

        Assert.Empty(nullListConfig.BlacklistedKeywords);

        var nullEntryConfig = new CardRewardEnchantConfig
        {
            EnchantChance = 0.5,
            BlacklistedKeywords = new List<string> { " Innate ", null!, "retain" }
        };

        nullEntryConfig.Normalize();

        Assert.Equal(new[] { "innate", "retain" }, nullEntryConfig.BlacklistedKeywords);
    }

    private static void ResetMenuDefaults()
    {
        CardRewardEnchantConfigMenu.Enabled = true;
        CardRewardEnchantConfigMenu.EnchantChancePercent = 100.0;
        CardRewardEnchantConfigMenu.LogRolls = true;
        CardRewardEnchantConfigMenu.InitializeFrom(new CardRewardEnchantConfig(), Array.Empty<string>());
    }

    private static string FindRepoFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not find {relativePath}");
    }
}
