using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class ZoomInConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("min_zoom_factor")] public uint MinZoomFactor { get; set; } = 40;
        [JsonPropertyName("max_zoom_factor")] public uint MaxZoomFactor { get; set; } = 70;
    }
}
