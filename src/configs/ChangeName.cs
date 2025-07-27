using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class ChangeNameConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
    }
}
