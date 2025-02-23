using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace RollTheDice
{
    public partial class RollTheDice : BasePlugin
    {
        private readonly List<string> _precacheModels = new List<string>
        {
            "models/chicken/chicken.vmdl",
            "particles/burning_fx/env_fire_tiny.vpcf",
            "models/food/fruits/banana01a.vmdl",
            "models/food/fruits/watermelon01a.vmdl",
            "models/food/vegetables/cabbage01a.vmdl",
            "models/food/vegetables/onion01a.vmdl",
            "models/food/vegetables/pepper01a.vmdl",
            "models/food/vegetables/potato01a.vmdl",
            "models/food/vegetables/zucchini01a.vmdl",
            "models/props/cs_office/plant01.vmdl",
            "models/props/de_vertigo/trafficcone_clean.vmdl",
            "models/generic/barstool_01/barstool_01.vmdl",
            "models/generic/fire_extinguisher_01/fire_extinguisher_01.vmdl",
            "models/hostage/hostage.vmdl",
            "models/ar_shoots/shoots_pottery_02.vmdl",
            "models/anubis/signs/anubis_info_panel_01.vmdl",
            "models/cs_italy/seating/chair/wood_chair_1.vmdl",
            "models/props_office/file_cabinet_03.vmdl",
            "models/props_plants/plantairport01.vmdl",
            "models/props_street/mail_dropbox.vmdl",
            "models/props_c17/furnituretable001a_static.vmdl",
        };

        private void OnServerPrecacheResources(ResourceManifest manifest)
        {
            foreach (var model in _precacheModels)
            {
                manifest.AddResource(model);
            }
        }
    }
}
