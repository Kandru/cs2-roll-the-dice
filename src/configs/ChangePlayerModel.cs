using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class ChangePlayerModelConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
    }
}
