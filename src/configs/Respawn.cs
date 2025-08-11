using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class RespawnConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("default_primary_weapon")] public string DefaultPrimaryWeapon { get; set; } = "weapon_m4a1";
        [JsonPropertyName("default_secondary_weapon")] public string DefaultSecondaryWeapon { get; set; } = "weapon_deagle";
    }
}
