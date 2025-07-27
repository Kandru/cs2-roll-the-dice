using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class PlayerOneHPConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
    }
}
