using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class HeadshotOnlyConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
    }
}
