using System;
using System.Text.RegularExpressions;

namespace Sls2Mods.Utils.CardText;

public static class CardTextRewrite
{
    private static readonly Regex ChineseDamageText = new(
        @"\u9020\u6210\s*(?<amount>[^\n\u3002\.]+?)\s*\u70b9?\u4f24\u5bb3",
        RegexOptions.Compiled);

    private static readonly Regex EnglishDamageText = new(
        @"\bDeal\s+(?<amount>[^\n\.]+?)\s+damage\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static string AppendSuffixOnce(string text, string suffix)
    {
        if (string.IsNullOrEmpty(suffix) || text.EndsWith(suffix, StringComparison.Ordinal))
        {
            return text;
        }

        return text + suffix;
    }

    public static string RewriteDamageAsPoison(string description, string fallbackHint)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return description;
        }

        if (!string.IsNullOrEmpty(fallbackHint) && description.Contains(fallbackHint, StringComparison.Ordinal))
        {
            return description;
        }

        var changed = false;
        var rewritten = ChineseDamageText.Replace(description, match =>
        {
            changed = true;
            return $"\u7ed9\u4e88 {match.Groups["amount"].Value.Trim()} \u5c42\u4e2d\u6bd2";
        });
        rewritten = EnglishDamageText.Replace(rewritten, match =>
        {
            changed = true;
            return $"Apply {match.Groups["amount"].Value.Trim()} Poison";
        });

        if (changed || string.IsNullOrEmpty(fallbackHint))
        {
            return rewritten;
        }

        return description + "\n" + fallbackHint;
    }
}
