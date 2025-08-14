using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class InvisibleConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("percentage_visible")] public float PercentageVisible { get; set; } = 0.5f;
    }
}
