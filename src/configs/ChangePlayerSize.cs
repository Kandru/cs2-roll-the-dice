using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class ChangePlayerSizeConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("min_size")] public float MinSize { get; set; } = 0.5f;
        [JsonPropertyName("max_size")] public float MaxSize { get; set; } = 1.5f;
        [JsonPropertyName("min_change_amount")] public float MinChangeAmount { get; set; } = 0.3f;
        [JsonPropertyName("adjust_health")] public bool AdjustHealth { get; set; } = true;
    }
}
