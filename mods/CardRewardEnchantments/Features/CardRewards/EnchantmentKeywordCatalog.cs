using System;
using System.Reflection;
using MegaCrit.Sts2.Core.Models;

namespace CardRewardEnchantments.Features.CardRewards;

public sealed class EnchantmentKeywordCatalog
{
    public static IReadOnlyList<string> FallbackKeywords { get; } = new[] { "adroit", "swift", "vigorous" };

    private EnchantmentKeywordCatalog(IReadOnlyList<EnchantmentDefinition> definitions)
    {
        Definitions = definitions;
        Keywords = definitions.Select(definition => definition.Keyword).ToList();
    }

    public IReadOnlyList<string> Keywords { get; }

    public IReadOnlyList<EnchantmentDefinition> Definitions { get; }

    public static EnchantmentKeywordCatalog Create(Action<string> log)
    {
        try
        {
            var discovered = DiscoverDefinitions();
            if (discovered.Count > 0)
            {
                log($"EnchantmentKeywordCatalog: discovered {discovered.Count} enchantment keywords");
                return new EnchantmentKeywordCatalog(discovered);
            }

            return FromFallback(log, "dynamic discovery returned no keywords");
        }
        catch (Exception ex)
        {
            return FromFallback(log, $"dynamic discovery failed: {ex.Message}");
        }
    }

    public static EnchantmentKeywordCatalog FromKeywords(IEnumerable<string> keywords, Action<string> log)
    {
        var definitions = Normalize(keywords)
            .Select(keyword => new EnchantmentDefinition(keyword))
            .ToList();
        if (definitions.Count == 0)
        {
            return FromFallback(log, "no normalized keywords remain");
        }

        return new EnchantmentKeywordCatalog(definitions);
    }

    private static EnchantmentKeywordCatalog FromFallback(Action<string> log, string reason)
    {
        var definitions = Normalize(FallbackKeywords)
            .Select(keyword => new EnchantmentDefinition(keyword))
            .ToList();
        log($"EnchantmentKeywordCatalog: using {definitions.Count} fallback keywords ({reason})");
        return new EnchantmentKeywordCatalog(definitions);
    }

    private static List<EnchantmentDefinition> DiscoverDefinitions()
    {
        var definitions = typeof(EnchantmentModel).Assembly
            .GetTypes()
            .Where(type => type != typeof(EnchantmentModel))
            .Where(type => typeof(EnchantmentModel).IsAssignableFrom(type))
            .Where(type => !type.IsAbstract)
            .Select(type => new EnchantmentDefinition(ModelDb.GetEntry(type).ToLowerInvariant(), type));

        return Normalize(definitions);
    }

    private static List<string> Normalize(IEnumerable<string> keywords)
    {
        return keywords
            .Where(keyword => keyword is not null)
            .Select(keyword => keyword!.Trim().ToLowerInvariant())
            .Where(keyword => !string.IsNullOrWhiteSpace(keyword))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(keyword => keyword, StringComparer.Ordinal)
            .ToList();
    }

    private static List<EnchantmentDefinition> Normalize(IEnumerable<EnchantmentDefinition> definitions)
    {
        return definitions
            .Where(definition => definition is not null)
            .Select(definition => definition with
            {
                Keyword = definition.Keyword.Trim().ToLowerInvariant()
            })
            .Where(definition => !string.IsNullOrWhiteSpace(definition.Keyword))
            .GroupBy(definition => definition.Keyword, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(definition => definition.Keyword, StringComparer.Ordinal)
            .ToList();
    }
}
