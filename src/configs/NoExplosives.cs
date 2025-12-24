using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class NoExplosivesConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("swap_delay")] public float SwapDelay { get; set; } = 0.1f;
        [JsonPropertyName("model_scale")] public float ModelScale { get; set; } = 1f;
        public List<string> RandomModels { get; set; } = [
            "models/food/fruits/banana01a.vmdl",
            "models/food/vegetables/onion01a.vmdl",
            "models/food/vegetables/pepper01a.vmdl",
            "models/food/vegetables/potato01a.vmdl",
            "models/food/vegetables/zucchini01a.vmdl"
        ];
    }
}
