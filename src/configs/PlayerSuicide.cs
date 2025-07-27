using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class PlayerSuicideConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
    }
}
