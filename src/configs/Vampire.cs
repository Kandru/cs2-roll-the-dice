using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class VampireConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("max_health")] public int MaxHealth { get; set; } = 200;
    }
}
