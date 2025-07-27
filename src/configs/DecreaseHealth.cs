using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class DecreaseHealthConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("min_health")] public int MinHealth { get; set; } = 10;
        [JsonPropertyName("max_health")] public int MaxHealth { get; set; } = 30;
        [JsonPropertyName("prevent_death")] public bool PreventDeath { get; set; } = true;
    }
}
