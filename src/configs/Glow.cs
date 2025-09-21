using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class GlowConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
    }
}
