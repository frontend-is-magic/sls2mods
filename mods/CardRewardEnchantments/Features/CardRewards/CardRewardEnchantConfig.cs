using System;
using System.Text.Json.Serialization;
using Sls2Mods.Utils.Config;

namespace CardRewardEnchantments.Features.CardRewards;

public sealed class CardRewardEnchantConfig : IModConfig
{
    [JsonPropertyName("schema_version")]
    public int SchemaVersion { get; set; } = 1;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("enchant_chance")]
    public double EnchantChance { get; set; } = 1.0;

    [JsonPropertyName("blacklisted_keywords")]
    public List<string> BlacklistedKeywords { get; set; } = new();

    [JsonPropertyName("log_rolls")]
    public bool LogRolls { get; set; } = true;

    public CardRewardEnchantConfig Normalize()
    {
        EnchantChance = Clamp01(EnchantChance);
        BlacklistedKeywords = (BlacklistedKeywords ?? new List<string>())
            .Where(keyword => keyword is not null)
            .Select(keyword => keyword!.Trim().ToLowerInvariant())
            .Where(keyword => !string.IsNullOrWhiteSpace(keyword))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(keyword => keyword, StringComparer.Ordinal)
            .ToList();
        return this;
    }

    private static double Clamp01(double value)
    {
        if (double.IsNaN(value))
        {
            return 0;
        }

        return Math.Clamp(value, 0, 1);
    }
}
