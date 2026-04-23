using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class WeirdGrenadesConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("min_detonate_time")] public float MinDetonateTime { get; set; } = 0.1f;
        [JsonPropertyName("max_detonate_time")] public float MaxDetonateTime { get; set; } = 10.0f;
        [JsonPropertyName("min_smokeduration")] public int MinSmokeduration { get; set; } = 1;
        [JsonPropertyName("max_smokeduration")] public int MaxSmokeduration { get; set; } = 19;
        [JsonPropertyName("min_blindduration")] public float MinBlindduration { get; set; } = 0.1f;
        [JsonPropertyName("max_blindduration")] public float MaxBlindduration { get; set; } = 5.0f;
    }
}
