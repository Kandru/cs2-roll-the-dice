using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class BigTaserBatteryConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("min_batteries")] public int MinAmount { get; set; } = 3;
        [JsonPropertyName("max_batteries")] public int MaxAmount { get; set; } = 10;
    }
}
