using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class StripWeaponsConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("disable_buymenu")] public bool DisableBuymenu { get; set; } = true;
    }
}
