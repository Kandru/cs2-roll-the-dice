using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class ReturnToSenderConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
    }
}
