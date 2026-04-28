using BaseLib.Config;

namespace AllSnakebite;

public sealed class AllSnakebiteConfigMenu : SimpleModConfig
{
    private readonly AllSnakebiteConfig? _fallback;
    private readonly Action<AllSnakebiteConfig>? _persist;

    public AllSnakebiteConfigMenu(
        AllSnakebiteConfig? fallback = null,
        Action<AllSnakebiteConfig>? persist = null)
    {
        _fallback = fallback;
        _persist = persist;
        if (_persist != null)
        {
            ConfigChanged += OnConfigChanged;
        }
    }

    public static bool Enabled { get; set; } = true;

    public static void InitializeFrom(AllSnakebiteConfig config)
    {
        Enabled = config.Normalize().Enabled;
    }

    public static AllSnakebiteConfig ToRuntimeConfig(AllSnakebiteConfig? fallback = null)
    {
        fallback ??= new AllSnakebiteConfig();
        fallback.Enabled = Enabled;
        return fallback.Normalize();
    }

    private void OnConfigChanged(object? sender, EventArgs args)
    {
        _persist?.Invoke(ToRuntimeConfig(_fallback));
    }
}
