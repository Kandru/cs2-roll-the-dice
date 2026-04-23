using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class NoHeadshotConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
    }
}
