using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class MillionaireConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("money")] public int Money { get; set; } = 20000;
    }
}
