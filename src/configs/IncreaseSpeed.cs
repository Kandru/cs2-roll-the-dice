using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class IncreaseSpeedConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("min_speed")] public float MinSpeed { get; set; } = 1.5f;
        [JsonPropertyName("max_speed")] public float MaxSpeed { get; set; } = 2f;
        [JsonPropertyName("reset_on_hostage_rescue")] public bool ResetOnHostageRescue { get; set; } = true;
    }
}
