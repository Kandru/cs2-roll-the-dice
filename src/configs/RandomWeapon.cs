using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class RandomWeaponConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("disable_buymenu")] public bool DisableBuymenu { get; set; } = true;
        [JsonPropertyName("random_secondary_and_primary_weapon")] public bool RandomSecondaryAndPrimaryWeapon { get; set; } = true;
        [JsonPropertyName("primary_weapons")]
        public List<string> PrimaryWeapons { get; set; } = [
            "weapon_ak47",
            "weapon_aug",
            "weapon_awp",
            "weapon_bizon",
            "weapon_famas",
            "weapon_g3sg1",
            "weapon_galilar",
            "weapon_m249",
            "weapon_m4a1",
            "weapon_m4a1_silencer",
            "weapon_mac10",
            "weapon_mag7",
            "weapon_mp5sd",
            "weapon_mp7",
            "weapon_mp9",
            "weapon_negev",
            "weapon_nova",
            "weapon_p90",
            "weapon_sawedoff",
            "weapon_scar20",
            "weapon_sg556",
            "weapon_ssg08",
            "weapon_ump45",
            "weapon_xm1014",
        ];
        [JsonPropertyName("secondary_weapons")]
        public List<string> SecondaryWeapons { get; set; } = [
            "weapon_cz75a",
            "weapon_deagle",
            "weapon_elite",
            "weapon_fiveseven",
            "weapon_glock",
            "weapon_p250",
            "weapon_revolver",
            "weapon_tec9",
            "weapon_usp_silencer",
            "weapon_hkp2000",
            "weapon_taser",
        ];
    }
}
