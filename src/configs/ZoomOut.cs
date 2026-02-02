using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class ZoomOutConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("min_zoom_factor")] public uint MinZoomFactor { get; set; } = 100;
        [JsonPropertyName("max_zoom_factor")] public uint MaxZoomFactor { get; set; } = 150;
    }
}
