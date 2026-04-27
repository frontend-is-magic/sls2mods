using CardRewardEnchantments.Features.CardRewards;
using Xunit;

namespace MapNodeChanger.Tests;

public sealed class EnchantmentKeywordCatalogTests
{
    [Fact]
    public void FromKeywordsNormalizesDeduplicatesAndSortsKeywords()
    {
        var catalog = EnchantmentKeywordCatalog.FromKeywords(
            new[] { " Swift ", "adroit", "", "SWIFT", "vigorous" },
            _ => { });

        Assert.Equal(new[] { "adroit", "swift", "vigorous" }, catalog.Keywords);
    }

    [Fact]
    public void FromKeywordsUsesRealEnchantmentFallbacksWhenNoKeywordsRemain()
    {
        var logs = new List<string>();

        var catalog = EnchantmentKeywordCatalog.FromKeywords(new[] { "", " " }, logs.Add);

        Assert.Equal(new[] { "adroit", "swift", "vigorous" }, catalog.Keywords);
        Assert.Contains(logs, message => message.Contains("fallback keywords"));
    }

    [Fact]
    public void FallbackKeywordsExposeDiscoveredRealEnchantments()
    {
        Assert.Equal(new[] { "adroit", "swift", "vigorous" }, EnchantmentKeywordCatalog.FallbackKeywords);
    }
}
