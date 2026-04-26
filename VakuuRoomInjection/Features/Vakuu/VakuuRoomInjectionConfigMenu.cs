using BaseLib.Config;

namespace MapNodeChanger.Features.Vakuu;

public sealed class VakuuRoomInjectionConfigMenu : SimpleModConfig
{
    public static AncientTarget AncientTarget { get; set; } = AncientTarget.Vakuu;

    [ConfigSlider(0.0, 100.0, 0.1, Format = "{0:0.0}%")]
    public static double OtherRoomChancePercent { get; set; } = 6.6;

    [ConfigSlider(0.0, 100.0, 0.1, Format = "{0:0.0}%")]
    public static double UnknownRoomChancePercent { get; set; } = 66.0;

    public static VakuuInjectionConfig ToRuntimeConfig(VakuuInjectionConfig? fallback = null)
    {
        fallback ??= new VakuuInjectionConfig();
        fallback.AncientTarget = AncientTarget;
        fallback.OtherRoomChance = PercentToProbability(OtherRoomChancePercent);
        fallback.UnknownRoomChance = PercentToProbability(UnknownRoomChancePercent);
        return fallback.Normalize();
    }

    private static double PercentToProbability(double percent)
    {
        if (double.IsNaN(percent))
        {
            return 0;
        }

        return Math.Clamp(percent, 0, 100) / 100.0;
    }
}
