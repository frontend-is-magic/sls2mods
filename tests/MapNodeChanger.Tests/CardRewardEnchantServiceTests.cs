using CardRewardEnchantments.Features.CardRewards;
using Xunit;

namespace MapNodeChanger.Tests;

public sealed class CardRewardEnchantServiceTests
{
    [Fact]
    public void ApplyToRewardCardsAppliesAllowedKeywordToUnenchantedCards()
    {
        var adapter = new FakeAdapter();
        var service = CreateService(
            new CardRewardEnchantConfig { EnchantChance = 1.0 },
            EnchantmentKeywordCatalog.FromKeywords(new[] { "adroit" }, _ => { }),
            adapter);
        var cards = new[] { new RewardCard(), new RewardCard() };

        service.ApplyToRewardCards(cards, new Random(123), "reward-1");

        Assert.Equal(new[] { "adroit", "adroit" }, cards.Select(card => card.Enchantment));
    }

    [Fact]
    public void ApplyToRewardCardsSkipsDisabledConfig()
    {
        var adapter = new FakeAdapter();
        var service = CreateService(
            new CardRewardEnchantConfig { Enabled = false, EnchantChance = 1.0 },
            EnchantmentKeywordCatalog.FromKeywords(new[] { "adroit" }, _ => { }),
            adapter);
        var card = new RewardCard();

        service.ApplyToRewardCards(new[] { card }, new Random(123), "reward-1");

        Assert.Null(card.Enchantment);
        Assert.Empty(adapter.AppliedKeywords);
    }

    [Fact]
    public void ApplyToRewardCardsSkipsCardsThatAlreadyHaveEnchantment()
    {
        var adapter = new FakeAdapter();
        var service = CreateService(
            new CardRewardEnchantConfig { EnchantChance = 1.0 },
            EnchantmentKeywordCatalog.FromKeywords(new[] { "adroit" }, _ => { }),
            adapter);
        var card = new RewardCard { Enchantment = "swift" };

        service.ApplyToRewardCards(new[] { card }, new Random(123), "reward-1");

        Assert.Equal("swift", card.Enchantment);
        Assert.Empty(adapter.AppliedKeywords);
    }

    [Fact]
    public void ApplyToRewardCardsRespectsChance()
    {
        var adapter = new FakeAdapter();
        var service = CreateService(
            new CardRewardEnchantConfig { EnchantChance = 0.0 },
            EnchantmentKeywordCatalog.FromKeywords(new[] { "adroit" }, _ => { }),
            adapter);
        var card = new RewardCard();

        service.ApplyToRewardCards(new[] { card }, new Random(123), "reward-1");

        Assert.Null(card.Enchantment);
        Assert.Empty(adapter.AppliedKeywords);
    }

    [Fact]
    public void ApplyToRewardCardsExcludesBlacklistedKeywords()
    {
        var adapter = new FakeAdapter();
        var service = CreateService(
            new CardRewardEnchantConfig
            {
                EnchantChance = 1.0,
                BlacklistedKeywords = new List<string> { "adroit", "swift" }
            },
            EnchantmentKeywordCatalog.FromKeywords(new[] { "adroit", "swift", "vigorous" }, _ => { }),
            adapter);
        var card = new RewardCard();

        service.ApplyToRewardCards(new[] { card }, new Random(123), "reward-1");

        Assert.Equal("vigorous", card.Enchantment);
    }

    [Fact]
    public void ApplyToRewardCardsLogsWhenNoKeywordsRemain()
    {
        var logs = new List<string>();
        var adapter = new FakeAdapter();
        var service = CreateService(
            new CardRewardEnchantConfig
            {
                EnchantChance = 1.0,
                BlacklistedKeywords = new List<string> { "adroit" },
                LogRolls = true
            },
            EnchantmentKeywordCatalog.FromKeywords(new[] { "adroit" }, _ => { }),
            adapter,
            logs.Add);
        var card = new RewardCard();

        service.ApplyToRewardCards(new[] { card }, new Random(123), "reward-1");

        Assert.Null(card.Enchantment);
        Assert.Contains(logs, message => message.Contains("no allowed enchantment keywords"));
    }

    [Fact]
    public void ApplyToRewardCardsLogsAdapterFailure()
    {
        var logs = new List<string>();
        var adapter = new FakeAdapter { FailureReason = "cannot enchant" };
        var service = CreateService(
            new CardRewardEnchantConfig { EnchantChance = 1.0, LogRolls = true },
            EnchantmentKeywordCatalog.FromKeywords(new[] { "adroit" }, _ => { }),
            adapter,
            logs.Add);

        service.ApplyToRewardCards(new[] { new RewardCard() }, new Random(123), "reward-1");

        Assert.Contains(logs, message => message.Contains("failed to apply adroit"));
        Assert.Contains(logs, message => message.Contains("cannot enchant"));
    }

    private static CardRewardEnchantService CreateService(
        CardRewardEnchantConfig config,
        EnchantmentKeywordCatalog catalog,
        FakeAdapter adapter,
        Action<string>? log = null)
    {
        return new CardRewardEnchantService(() => config, catalog, adapter, log ?? (_ => { }));
    }

    private sealed class RewardCard
    {
        public string? Enchantment { get; set; }
    }

    private sealed class FakeAdapter : ICardRewardEnchantAdapter
    {
        public string? FailureReason { get; set; }

        public List<string> AppliedKeywords { get; } = new();

        public bool HasEnchantment(object card)
        {
            return ((RewardCard)card).Enchantment != null;
        }

        public bool TryApplyEnchantment(object card, string keyword, out string failureReason)
        {
            if (FailureReason != null)
            {
                failureReason = FailureReason;
                return false;
            }

            ((RewardCard)card).Enchantment = keyword;
            AppliedKeywords.Add(keyword);
            failureReason = string.Empty;
            return true;
        }
    }
}
