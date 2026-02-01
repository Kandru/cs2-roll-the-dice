using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class LoudStepsConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("sound_event_name")] public string SoundEventName { get; set; } = "Base.Footstep";
    }
}
