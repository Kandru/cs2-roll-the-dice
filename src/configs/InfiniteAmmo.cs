using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class InfiniteAmmoConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
    }
}
