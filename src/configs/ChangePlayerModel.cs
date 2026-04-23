using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class ChangePlayerModelConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("ct_model")] public string CTModel { get; set; } = "agents/models/tm_phoenix/tm_phoenix.vmdl";
        [JsonPropertyName("t_model")] public string TModel { get; set; } = "agents/models/ctm_sas/ctm_sas.vmdl";
    }
}
