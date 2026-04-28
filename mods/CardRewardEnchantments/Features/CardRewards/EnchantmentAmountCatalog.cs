using System;

namespace CardRewardEnchantments.Features.CardRewards;

public static class EnchantmentAmountCatalog
{
    private static readonly IReadOnlyDictionary<string, decimal> Amounts =
        new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["adroit"] = 3m,
            ["clone"] = 4m,
            ["momentum"] = 5m,
            ["nimble"] = 2m,
            ["sharp"] = 3m,
            ["swift"] = 3m,
            ["vigorous"] = 8m
        };

    public static decimal GetAmount(string keyword)
    {
        var normalized = keyword.Trim().ToLowerInvariant();
        return Amounts.TryGetValue(normalized, out var amount)
            ? amount
            : 1m;
    }
}
