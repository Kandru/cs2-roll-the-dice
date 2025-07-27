using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class PlayerLowGravityConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("gravity_scale")] public float GravityScale { get; set; } = 0.4f;
    }
}
