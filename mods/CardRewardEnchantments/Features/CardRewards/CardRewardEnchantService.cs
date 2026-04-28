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

        var candidates = _catalog.Definitions
            .Where(definition => !config.BlacklistedKeywords.Contains(definition.Keyword, StringComparer.OrdinalIgnoreCase))
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

            var compatible = new List<EnchantmentDefinition>();
            foreach (var candidate in candidates)
            {
                if (_adapter.CanEnchant(card, candidate, out var compatibilityFailure))
                {
                    compatible.Add(candidate);
                }
                else if (config.LogRolls)
                {
                    _log($"CardRewardEnchant: skipped {candidate.Keyword} for {rewardKey} cardIndex={index}: {compatibilityFailure}");
                }
            }
            if (compatible.Count == 0)
            {
                if (config.LogRolls)
                {
                    _log($"CardRewardEnchant: no compatible enchantment keywords for {rewardKey} cardIndex={index}");
                }

                index++;
                continue;
            }

            var definition = compatible[rng.Next(compatible.Count)];
            if (!_adapter.TryApplyEnchantment(card, definition, out var failureReason) && config.LogRolls)
            {
                _log($"CardRewardEnchant: failed to apply {definition.Keyword} to {rewardKey} cardIndex={index}: {failureReason}");
            }

            index++;
        }
    }
}
