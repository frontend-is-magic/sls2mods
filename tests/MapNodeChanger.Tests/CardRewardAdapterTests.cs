using CardRewardEnchantments.Features.CardRewards;
using Xunit;

namespace MapNodeChanger.Tests;

public sealed class CardRewardAdapterTests
{
    [Fact]
    public void TryApplyEnchantmentDoesNotLogAdapterFailures()
    {
        var adapter = new CardRewardAdapter();

        var applied = adapter.TryApplyEnchantment(new object(), "adroit", out var failureReason);

        Assert.False(applied);
        Assert.Contains("no supported enchantment API", failureReason);
    }

    [Fact]
    public void HasEnchantmentReturnsTrueWhenSingleEnchantmentGetterThrows()
    {
        var adapter = new CardRewardAdapter();

        var hasEnchantment = adapter.HasEnchantment(new ThrowingSingleEnchantmentCard());

        Assert.True(hasEnchantment);
    }

    [Fact]
    public void HasEnchantmentReturnsTrueWhenCollectionEnchantmentGetterThrows()
    {
        var adapter = new CardRewardAdapter();

        var hasEnchantment = adapter.HasEnchantment(new ThrowingCollectionEnchantmentCard());

        Assert.True(hasEnchantment);
    }

    [Fact]
    public void TryApplyEnchantmentUsesRealCardCmdApiPath()
    {
        var source = File.ReadAllText(FindRepoFile("mods/CardRewardEnchantments/Features/CardRewards/CardRewardAdapter.cs"));

        Assert.Contains("CardCmd.Enchant", source);
        Assert.Contains("ModelDb.GetById<EnchantmentModel>", source);
        Assert.Contains("ModelId.SlugifyCategory<EnchantmentModel>()", source);
        Assert.Contains("definition.Keyword.ToUpperInvariant()", source);
        Assert.Contains("EnchantmentAmountCatalog.GetAmount(definition.Keyword)", source);
        Assert.Contains("CardCmd.Enchant(enchantment, cardModel, amount)", source);
        Assert.DoesNotContain("CardCmd.Enchant(enchantment, cardModel, 1m)", source);
        Assert.Contains("CanEnchant", source);
    }

    [Fact]
    public void TryApplyEnchantmentKeepsSimpleCardLocalStringFallback()
    {
        var adapter = new CardRewardAdapter();
        var card = new CardWithLocalMethod();

        var applied = adapter.TryApplyEnchantment(card, "adroit", out var failureReason);

        Assert.True(applied);
        Assert.Equal("adroit", card.Enchantment);
        Assert.Equal(string.Empty, failureReason);
    }

    [Fact]
    public void ExtractRewardCardsFailsClosedForIncompatibleObjects()
    {
        var adapter = new CardRewardAdapter();

        var cards = adapter.ExtractRewardCards(new object(), Task.CompletedTask);

        Assert.Empty(cards);
    }

    [Fact]
    public void BuildRewardKeyUsesStableCardContent()
    {
        var adapter = new CardRewardAdapter();

        var first = adapter.BuildRewardKey(new object[] { new CardWithId("strike"), new CardWithId("defend") });
        var second = adapter.BuildRewardKey(new object[] { new CardWithId("strike"), new CardWithId("defend") });

        Assert.Equal(first, second);
        Assert.Contains("strike", first);
        Assert.Contains("defend", first);
    }

    private sealed class ThrowingSingleEnchantmentCard
    {
        public string? Enchantment => throw new InvalidOperationException("getter failed");
    }

    private sealed class CardWithId
    {
        public CardWithId(string id)
        {
            Id = id;
        }

        public string Id { get; }
    }

    private sealed class ThrowingCollectionEnchantmentCard
    {
        public IReadOnlyList<string> Enchantments => throw new InvalidOperationException("getter failed");
    }

    private sealed class CardWithLocalMethod
    {
        public string? Enchantment { get; private set; }

        public void AddEnchantment(string keyword)
        {
            Enchantment = keyword;
        }
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
