using System;

namespace CardRewardEnchantments.Features.CardRewards;

public sealed class CardRewardEnchantService
{
    private readonly Func<CardRewardEnchantConfig> _getConfig;
    private readonly EnchantmentKeywordCatalog _catalog;
    private readonly ICardRewardEnchantAdapter _adapter;
    private readonly Action<string> _log;

    public CardRewardEnchantService(
        Func<CardRewardEnchantConfig> getConfig,
        EnchantmentKeywordCatalog catalog,
        ICardRewardEnchantAdapter adapter,
        Action<string> log)
    {
        _getConfig = getConfig;
        _catalog = catalog;
        _adapter = adapter;
        _log = log;
    }

    public void ApplyToRewardCards(IEnumerable<object> cards, Random rng, string rewardKey)
    {
        var config = _getConfig().Normalize();
        if (!config.Enabled)
        {
            return;
        }

        var candidates = _catalog.Keywords
            .Where(keyword => !config.BlacklistedKeywords.Contains(keyword, StringComparer.OrdinalIgnoreCase))
            .ToList();
        if (candidates.Count == 0)
        {
            if (config.LogRolls)
            {
                _log($"CardRewardEnchant: no allowed enchantment keywords for {rewardKey}");
            }

            return;
        }

        var index = 0;
        foreach (var card in cards)
        {
            if (_adapter.HasEnchantment(card))
            {
                index++;
                continue;
            }

            var roll = rng.NextDouble();
            if (roll >= config.EnchantChance)
            {
                index++;
                continue;
            }

            var keyword = candidates[rng.Next(candidates.Count)];
            if (!_adapter.TryApplyEnchantment(card, keyword, out var failureReason) && config.LogRolls)
            {
                _log($"CardRewardEnchant: failed to apply {keyword} to {rewardKey} cardIndex={index}: {failureReason}");
            }

            index++;
        }
    }
}
