using System.Text.Json.Serialization;
using Sls2Mods.Utils.Config;

namespace AllSnakebite;

public sealed class AllSnakebiteConfig : IModConfig
{
    public const int CurrentSchemaVersion = 1;

    [JsonPropertyName("schema_version")]
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    public AllSnakebiteConfig Normalize()
    {
        if (SchemaVersion <= 0)
        {
            SchemaVersion = CurrentSchemaVersion;
        }

        return this;
    }
}
