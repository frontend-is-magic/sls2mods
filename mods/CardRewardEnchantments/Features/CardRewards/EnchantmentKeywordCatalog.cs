using System;
using System.Collections;
using System.Reflection;

namespace CardRewardEnchantments.Features.CardRewards;

public sealed class EnchantmentKeywordCatalog
{
    public static IReadOnlyList<string> FallbackKeywords { get; } = new[] { "adroit", "swift", "vigorous" };

    private EnchantmentKeywordCatalog(IReadOnlyList<string> keywords)
    {
        Keywords = keywords;
    }

    public IReadOnlyList<string> Keywords { get; }

    public static EnchantmentKeywordCatalog Create(Action<string> log)
    {
        try
        {
            var discovered = DiscoverKeywords();
            if (discovered.Count > 0)
            {
                log($"EnchantmentKeywordCatalog: discovered {discovered.Count} enchantment keywords");
                return new EnchantmentKeywordCatalog(discovered);
            }

            log("EnchantmentKeywordCatalog: dynamic discovery returned no keywords, using fallback keywords");
        }
        catch (Exception ex)
        {
            log($"EnchantmentKeywordCatalog: dynamic discovery failed, using fallback keywords: {ex.Message}");
        }

        return FromKeywords(FallbackKeywords, log);
    }

    public static EnchantmentKeywordCatalog FromKeywords(IEnumerable<string> keywords, Action<string> log)
    {
        var normalized = Normalize(keywords);
        if (normalized.Count == 0)
        {
            normalized = Normalize(FallbackKeywords);
            log($"EnchantmentKeywordCatalog: using {normalized.Count} fallback keywords");
        }

        return new EnchantmentKeywordCatalog(normalized);
    }

    private static List<string> DiscoverKeywords()
    {
        var modelDbType = FindType("MegaCrit.Sts2.Core.Models.ModelDb")
            ?? throw new InvalidOperationException("ModelDb type was not found");
        var debugEnchantments = modelDbType.GetProperty("DebugEnchantments", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("ModelDb.DebugEnchantments was not found");

        if (debugEnchantments.GetValue(null) is not IEnumerable enchantments)
        {
            return new List<string>();
        }

        var keywords = new List<string>();
        foreach (var enchantment in enchantments)
        {
            var id = enchantment.GetType().GetProperty("Id")?.GetValue(enchantment);
            var entry = id?.GetType().GetProperty("Entry")?.GetValue(id)?.ToString();
            if (entry != null)
            {
                keywords.Add(entry);
            }
        }

        return Normalize(keywords);
    }

    private static Type? FindType(string fullName)
    {
        return Type.GetType(fullName)
            ?? AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(fullName, throwOnError: false))
                .FirstOrDefault(type => type != null);
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
}
