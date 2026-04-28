using CardRewardEnchantments.Features.CardRewards;
using Xunit;

namespace MapNodeChanger.Tests;

public sealed class EnchantmentAmountCatalogTests
{
    [Theory]
    [InlineData("swift", 3)]
    [InlineData("vigorous", 8)]
    [InlineData("adroit", 3)]
    [InlineData("clone", 4)]
    [InlineData("nimble", 2)]
    [InlineData("sharp", 3)]
    [InlineData("momentum", 5)]
    [InlineData("glam", 1)]
    [InlineData("unknown", 1)]
    public void GetAmountUsesMaxObservedGameAmountOrDefault(string keyword, decimal expected)
    {
        Assert.Equal(expected, EnchantmentAmountCatalog.GetAmount(keyword));
    }

    [Fact]
    public void GetAmountNormalizesKeywords()
    {
        Assert.Equal(3m, EnchantmentAmountCatalog.GetAmount(" SWIFT "));
    }
}
