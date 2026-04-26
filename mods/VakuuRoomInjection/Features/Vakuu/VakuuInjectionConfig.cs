using System;
using System.Text.Json.Serialization;

namespace MapNodeChanger.Features.Vakuu;

public sealed class VakuuInjectionConfig
{
    [JsonPropertyName("schema_version")]
    public int SchemaVersion { get; set; } = 2;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("seed")]
    public int Seed { get; set; }

    [JsonPropertyName("ancient_target")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AncientTarget AncientTarget { get; set; } = AncientTarget.Vakuu;

    [JsonPropertyName("unknown_room_chance")]
    public double UnknownRoomChance { get; set; } = 0.66;

    [JsonPropertyName("other_room_chance")]
    public double OtherRoomChance { get; set; } = 0.066;

    [JsonPropertyName("replace_natural_ancient")]
    public bool ReplaceNaturalAncient { get; set; } = true;

    [JsonPropertyName("log_rolls")]
    public bool LogRolls { get; set; } = true;

    public VakuuInjectionConfig Normalize()
    {
        UnknownRoomChance = Clamp01(UnknownRoomChance);
        OtherRoomChance = Clamp01(OtherRoomChance);
        return this;
    }

    private static double Clamp01(double value)
    {
        if (double.IsNaN(value))
        {
            return 0;
        }

        return Math.Clamp(value, 0, 1);
    }
}
