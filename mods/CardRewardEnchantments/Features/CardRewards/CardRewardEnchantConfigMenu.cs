using BaseLib.Config;

namespace CardRewardEnchantments.Features.CardRewards;

public sealed class CardRewardEnchantConfigMenu : SimpleModConfig
{
    public static bool Enabled { get; set; } = true;

    [ConfigSlider(0.0, 100.0, 0.1, Format = "{0:0.0}%")]
    public static double EnchantChancePercent { get; set; } = 100.0;

    public static bool BlacklistInnate { get; set; }

    public static bool BlacklistRetain { get; set; }

    public static bool BlacklistEthereal { get; set; }

    public static CardRewardEnchantConfig ToRuntimeConfig(CardRewardEnchantConfig? fallback = null)
    {
        fallback ??= new CardRewardEnchantConfig();
        fallback.Enabled = Enabled;
        fallback.EnchantChance = PercentToProbability(EnchantChancePercent);
        fallback.BlacklistedKeywords = BuildBlacklist();
        return fallback.Normalize();
    }

    private static List<string> BuildBlacklist()
    {
        var blacklist = new List<string>();
        if (BlacklistInnate)
        {
            blacklist.Add("innate");
        }

        if (BlacklistRetain)
        {
            blacklist.Add("retain");
        }

        if (BlacklistEthereal)
        {
            blacklist.Add("ethereal");
        }

        return blacklist;
    }

    private static double PercentToProbability(double percent)
    {
        if (double.IsNaN(percent))
        {
            return 0;
        }

        return Math.Clamp(percent, 0, 100) / 100.0;
    }
}
