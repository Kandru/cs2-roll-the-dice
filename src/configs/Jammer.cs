using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class JammerConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
    }
}
