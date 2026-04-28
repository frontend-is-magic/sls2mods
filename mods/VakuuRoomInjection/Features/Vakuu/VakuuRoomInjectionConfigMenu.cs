using BaseLib.Config;
using BaseLib.Config.UI;
using Godot;
using Sls2Mods.Utils.Config;

namespace VakuuRoomInjection.Features.Vakuu;

public sealed class VakuuRoomInjectionConfigMenu : SimpleModConfig
{
    private readonly VakuuInjectionConfig? _fallback;
    private readonly Action<VakuuInjectionConfig>? _persist;

    public VakuuRoomInjectionConfigMenu(
        VakuuInjectionConfig? fallback = null,
        Action<VakuuInjectionConfig>? persist = null)
    {
        _fallback = fallback;
        _persist = persist;
        if (_persist != null)
        {
            ConfigChanged += OnConfigChanged;
        }
    }

    [ConfigHideInUI]
    public static bool Enabled { get; set; } = true;

    public static AncientTarget AncientTarget { get; set; } = AncientTarget.Vakuu;

    [ConfigSlider(0.0, 100.0, 0.1, Format = "{0:0.0}%")]
    public static double OtherRoomChancePercent { get; set; } = 6.6;

    [ConfigSlider(0.0, 100.0, 0.1, Format = "{0:0.0}%")]
    public static double UnknownRoomChancePercent { get; set; } = 66.0;

    public static bool ReplaceNaturalAncient { get; set; } = true;

    public static bool LogRolls { get; set; } = true;

    public override void SetupConfigUI(Control optionContainer)
    {
        AddEnabledOption(optionContainer);
        GenerateOptionsForAllProperties(optionContainer);
        ModMenuLocalization.LocalizeLabels(optionContainer, OptionLabels());
        AddRestoreDefaultsButton(optionContainer);
        ModMenuLocalization.LocalizeLabels(optionContainer, OptionLabels());
        SetupFocusNeighbors(optionContainer);
    }

    public static void InitializeFrom(VakuuInjectionConfig config)
    {
        var normalized = config.Normalize();
        Enabled = normalized.Enabled;
        AncientTarget = normalized.AncientTarget;
        OtherRoomChancePercent = normalized.OtherRoomChance * 100.0;
        UnknownRoomChancePercent = normalized.UnknownRoomChance * 100.0;
        ReplaceNaturalAncient = normalized.ReplaceNaturalAncient;
        LogRolls = normalized.LogRolls;
    }

    public static VakuuInjectionConfig ToRuntimeConfig(VakuuInjectionConfig? fallback = null)
    {
        fallback ??= new VakuuInjectionConfig();
        fallback.Enabled = Enabled;
        fallback.AncientTarget = AncientTarget;
        fallback.OtherRoomChance = PercentToProbability(OtherRoomChancePercent);
        fallback.UnknownRoomChance = PercentToProbability(UnknownRoomChancePercent);
        fallback.ReplaceNaturalAncient = ReplaceNaturalAncient;
        fallback.LogRolls = LogRolls;
        return fallback.Normalize();
    }

    private void AddEnabledOption(Control optionContainer)
    {
        var labelText = ModMenuLocalization.EnabledLabel();
        var tickbox = new NConfigBooleanTickbox(
            () => Enabled,
            value => Enabled = value,
            Changed);
        var label = CreateRawLabelControl(labelText, 28);
        var row = new NConfigOptionRow(ModPrefix, labelText, label, tickbox)
        {
            UniqueNameInOwner = true,
            Owner = optionContainer
        };
        optionContainer.AddChild(row, forceReadableName: false, Node.InternalMode.Disabled);
    }

    private static double PercentToProbability(double percent)
    {
        if (double.IsNaN(percent))
        {
            return 0;
        }

        return Math.Clamp(percent, 0, 100) / 100.0;
    }

    private static IReadOnlyDictionary<string, string> OptionLabels()
    {
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["AncientTarget"] = ModMenuLocalization.Text("Ancient target", "远古目标"),
            ["Ancient Target"] = ModMenuLocalization.Text("Ancient target", "远古目标"),
            ["OtherRoomChancePercent"] = ModMenuLocalization.Text("Other room chance", "其它房间概率"),
            ["Other Room Chance Percent"] = ModMenuLocalization.Text("Other room chance", "其它房间概率"),
            ["UnknownRoomChancePercent"] = ModMenuLocalization.Text("Unknown room chance", "未知房间概率"),
            ["Unknown Room Chance Percent"] = ModMenuLocalization.Text("Unknown room chance", "未知房间概率"),
            ["ReplaceNaturalAncient"] = ModMenuLocalization.Text("Replace natural ancient", "替换自然远古房"),
            ["Replace Natural Ancient"] = ModMenuLocalization.Text("Replace natural ancient", "替换自然远古房"),
            ["LogRolls"] = ModMenuLocalization.Text("Log rolls", "记录随机结果"),
            ["Log Rolls"] = ModMenuLocalization.Text("Log rolls", "记录随机结果"),
            ["Restore Defaults"] = ModMenuLocalization.Text("Restore defaults", "恢复默认"),
            ["Restore defaults"] = ModMenuLocalization.Text("Restore defaults", "恢复默认")
        };
    }

    private void OnConfigChanged(object? sender, EventArgs args)
    {
        var config = ToRuntimeConfig(_fallback);
        _persist?.Invoke(config);
    }
}
