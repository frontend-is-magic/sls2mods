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
        CardRewardEnchantConfigMenu.BlacklistInnate = true;
        CardRewardEnchantConfigMenu.BlacklistRetain = false;
        CardRewardEnchantConfigMenu.BlacklistEthereal = true;

        var config = CardRewardEnchantConfigMenu.ToRuntimeConfig();

        Assert.Equal(new[] { "ethereal", "innate" }, config.BlacklistedKeywords);
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
        CardRewardEnchantConfigMenu.BlacklistInnate = false;
        CardRewardEnchantConfigMenu.BlacklistRetain = false;
        CardRewardEnchantConfigMenu.BlacklistEthereal = false;
    }
}
