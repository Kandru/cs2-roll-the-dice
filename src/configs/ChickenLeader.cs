using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class ChickenLeaderConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("amount_chicken")] public int Amount { get; set; } = 16;
    }
}
