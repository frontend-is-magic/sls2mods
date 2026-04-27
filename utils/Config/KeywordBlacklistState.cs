using System;

namespace Sls2Mods.Utils.Config;

public sealed class KeywordBlacklistState
{
    private readonly SortedDictionary<string, bool> _states = new(StringComparer.Ordinal);

    public IReadOnlyList<string> Keywords => _states.Keys.ToList();

    public void Initialize(IEnumerable<string> keywords, IEnumerable<string>? blacklistedKeywords)
    {
        _states.Clear();
        var blacklist = Normalize(blacklistedKeywords ?? Array.Empty<string>()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var keyword in Normalize(keywords))
        {
            _states[keyword] = blacklist.Contains(keyword);
        }
    }

    public bool IsBlacklisted(string keyword)
    {
        var normalized = NormalizeOne(keyword);
        return normalized != null && _states.TryGetValue(normalized, out var value) && value;
    }

    public void SetBlacklisted(string keyword, bool blacklisted)
    {
        var normalized = NormalizeOne(keyword);
        if (normalized == null)
        {
            return;
        }

        _states[normalized] = blacklisted;
    }

    public List<string> ToBlacklist()
    {
        return _states
            .Where(pair => pair.Value)
            .Select(pair => pair.Key)
            .ToList();
    }

    public static List<string> Normalize(IEnumerable<string> keywords)
    {
        return keywords
            .Where(keyword => keyword is not null)
            .Select(keyword => NormalizeOne(keyword!))
            .Where(keyword => keyword is not null)
            .Select(keyword => keyword!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(keyword => keyword, StringComparer.Ordinal)
            .ToList();
    }

    private static string? NormalizeOne(string keyword)
    {
        var normalized = keyword.Trim().ToLowerInvariant();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
