using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class CutterConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
    }
}
