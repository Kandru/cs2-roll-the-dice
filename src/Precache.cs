using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace RollTheDice
{
    public partial class RollTheDice : BasePlugin
    {
        private static readonly HashSet<string> PrecacheModels =
        [
            "models/chicken/chicken.vmdl",
            "particles/burning_fx/env_fire_tiny.vpcf",
            // dice explosions (Martyrdom, ClusterGrenade, Airstrike, ExplosiveRounds): env_explosion draws no fireball in CS2
            Utils.Explosion.ParticleEffect,
            Utils.Explosion.SmallParticleEffect,
            Dices.ConfettiKills.ParticleEffect,
            // smoke cloud of NinjaSmoke and PlayDead
            Utils.Particle.Smoke,
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
            foreach (string model in PrecacheModels)
            {
                manifest.AddResource(model);
            }

            foreach (string model in _currentMapConfig.Dices.NoExplosives.RandomModels)
            {
                if (string.IsNullOrEmpty(model) || PrecacheModels.Contains(model))
                {
                    continue;
                }
                manifest.AddResource(model);
            }

            if (!string.IsNullOrEmpty(Config.Precache.SoundEventFile))
            {
                manifest.AddResource(Config.Precache.SoundEventFile);
            }
        }
    }
}
