using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class PlayerHealthBarConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
    }
}
