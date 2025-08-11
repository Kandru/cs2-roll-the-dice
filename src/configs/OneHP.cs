using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class OneHPConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
    }
}
