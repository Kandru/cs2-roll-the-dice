using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class LongerFlashesConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("min_blindduration_factor")] public float MinBlinddurationFactor { get; set; } = 1.3f;
        [JsonPropertyName("max_blindduration_factor")] public float MaxBlinddurationFactor { get; set; } = 3.0f;
    }
}
