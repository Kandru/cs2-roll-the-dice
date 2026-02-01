using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class DamageMultiplierConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("min_multiplier")] public float MinMultiplier { get; set; } = 0.5f;
        [JsonPropertyName("max_multiplier")] public float MaxMultiPlier { get; set; } = 2.0f;
    }
}
