using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class SuicideConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
    }
}
