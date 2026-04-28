using System.Globalization;
using Godot;

namespace Sls2Mods.Utils.Config;

public static class ModMenuLocalization
{
    public static string Text(string english, string simplifiedChinese)
    {
        return Text(english, simplifiedChinese, CurrentLocale());
    }

    public static string Text(string english, string simplifiedChinese, string? locale)
    {
        return IsChineseLocale(locale) ? simplifiedChinese : english;
    }

    public static string EnabledLabel()
    {
        return EnabledLabel(CurrentLocale());
    }

    public static string EnabledLabel(string? locale)
    {
        return Text("Enabled", "是否开启", locale);
    }

    public static void LocalizeLabels(Node root, IReadOnlyDictionary<string, string> labels)
    {
        ReplaceLabelText(root, labels);

        foreach (var child in root.GetChildren())
        {
            LocalizeLabels(child, labels);
        }
    }

    public static string CurrentLocale()
    {
        try
        {
            var locale = TranslationServer.GetLocale();
            if (!string.IsNullOrWhiteSpace(locale))
            {
                return locale;
            }
        }
        catch
        {
            // Godot can be unavailable in unit tests; fall back to .NET culture.
        }

        return CultureInfo.CurrentUICulture.Name;
    }

    private static bool IsChineseLocale(string? locale)
    {
        return !string.IsNullOrWhiteSpace(locale)
            && locale.StartsWith("zh", StringComparison.OrdinalIgnoreCase);
    }

    private static void ReplaceLabelText(Node node, IReadOnlyDictionary<string, string> labels)
    {
        switch (node)
        {
            case Label label when labels.TryGetValue(label.Text, out var text):
                label.Text = text;
                break;
            case RichTextLabel richTextLabel when labels.TryGetValue(richTextLabel.Text, out var text):
                richTextLabel.Text = text;
                break;
        }
    }
}
