using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class ResetOnReloadConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
    }
}
