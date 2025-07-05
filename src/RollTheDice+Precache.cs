using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace RollTheDice
{
    public partial class RollTheDice : BasePlugin
    {
        private readonly List<string> _precacheModels =
        [
            "models/chicken/chicken.vmdl",
            "particles/burning_fx/env_fire_tiny.vpcf",
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
            "models/props_fairgrounds/fairgrounds_flagpole01.vmdl",
            "models/props_foliage/mall_small_palm01.vmdl",
            "models/props_foliage/urban_pot_fancy01.vmdl",
            "models/props_interiors/copymachine01.vmdl",
            "models/props_interiors/trashcan01.vmdl",
        ];

        private void OnServerPrecacheResources(ResourceManifest manifest)
        {
            // check for additional models in DiceNoExplosives config
            Dictionary<string, object> config = GetDiceConfig("DiceNoExplosives");
            if (config.TryGetValue("models", out object? modelsObj) && modelsObj is List<object> models)
            {
                foreach (object model in models)
                {
                    if (model is Dictionary<string, object> modelDict
                        && modelDict.TryGetValue("Model", out object? modelName)
                        && modelName is string modelStr)
                    {
                        _precacheModels.Add(modelStr);
                    }
                }
            }
            // precache all models
            foreach (string model in _precacheModels)
            {
                manifest.AddResource(model);
            }
        }
    }
}
