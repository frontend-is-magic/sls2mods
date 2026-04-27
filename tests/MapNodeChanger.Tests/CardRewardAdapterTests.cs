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
    public void TryApplyEnchantmentDoesNotUseCardCmdReflectionPath()
    {
        var source = File.ReadAllText(FindRepoFile("mods/CardRewardEnchantments/Features/CardRewards/CardRewardAdapter.cs"));

        Assert.DoesNotContain("CardCmd", source);
        Assert.DoesNotContain("ModelDb", source);
    }

    [Fact]
    public void TryApplyEnchantmentUsesSimpleCardLocalStringMethod()
    {
        var adapter = new CardRewardAdapter();
        var card = new CardWithLocalMethod();

        var applied = adapter.TryApplyEnchantment(card, "adroit", out var failureReason);

        Assert.True(applied);
        Assert.Equal("adroit", card.Enchantment);
        Assert.Equal(string.Empty, failureReason);
    }

    private sealed class ThrowingSingleEnchantmentCard
    {
        public string? Enchantment => throw new InvalidOperationException("getter failed");
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
