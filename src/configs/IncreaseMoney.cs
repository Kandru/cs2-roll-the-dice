using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class IncreaseMoneyConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("min_money")] public int MinMoney { get; set; } = 100;
        [JsonPropertyName("max_money")] public int MaxMoney { get; set; } = 1000;
    }
}
