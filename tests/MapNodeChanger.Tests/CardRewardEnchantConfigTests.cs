using CardRewardEnchantments.Features.CardRewards;
using Xunit;

namespace MapNodeChanger.Tests;

public sealed class CardRewardEnchantConfigTests
{
    [Fact]
    public void MenuDefaultsConvertToRuntimeConfig()
    {
        CardRewardEnchantConfigMenu.Enabled = true;
        CardRewardEnchantConfigMenu.EnchantChancePercent = 100.0;
        CardRewardEnchantConfigMenu.BlacklistInnate = false;
        CardRewardEnchantConfigMenu.BlacklistRetain = false;
        CardRewardEnchantConfigMenu.BlacklistEthereal = false;

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
}
