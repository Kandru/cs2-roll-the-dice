using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class FogOfWarConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("color")] public string Color { get; set; } = "#a3a3a3";
        [JsonPropertyName("exponent")] public float Exponent { get; set; } = 0.05f;
        [JsonPropertyName("density")] public float Density { get; set; } = 1f;
        [JsonPropertyName("distance")] public int EndDistance { get; set; } = 10000;
        [JsonPropertyName("player_visibility")] public float PlayerVisibility { get; set; } = 1f;
    }
}
