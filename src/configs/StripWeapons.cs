using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class StripWeaponsConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
    }
}
