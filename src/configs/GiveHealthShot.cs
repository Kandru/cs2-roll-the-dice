using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class GiveHealthShotConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("min_shots")] public int MinShots { get; set; } = 1;
        [JsonPropertyName("max_shots")] public int MaxShots { get; set; } = 5;
    }
}
