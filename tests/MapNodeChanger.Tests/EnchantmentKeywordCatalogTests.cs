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

    [Fact]
    public void CreateDiscoversAllConcreteEnchantmentModelSubtypes()
    {
        var logs = new List<string>();

        var catalog = EnchantmentKeywordCatalog.Create(logs.Add);

        Assert.Contains("clone", catalog.Keywords);
        Assert.Contains("corrupted", catalog.Keywords);
        Assert.Contains("deprecated_enchantment", catalog.Keywords);
        Assert.Contains("glam", catalog.Keywords);
        Assert.Contains("goopy", catalog.Keywords);
        Assert.Contains("mock_free_enchantment", catalog.Keywords);
        Assert.Contains("vigorous", catalog.Keywords);
        Assert.True(catalog.Keywords.Count > EnchantmentKeywordCatalog.FallbackKeywords.Count);
        Assert.All(catalog.Definitions, definition => Assert.NotNull(definition.EnchantmentType));
        Assert.DoesNotContain(logs, message => message.Contains("fallback keywords"));
    }
}
