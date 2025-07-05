using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace RollTheDice
{
    public partial class RollTheDice : BasePlugin
    {
        private readonly List<CCSPlayerPawn> _playersWithoutExplosives = [];
        private readonly HashSet<string> _grenadeProjectiles =
        [
            "smokegrenade_projectile",
            "hegrenade_projectile",
            "molotov_projectile",
            "decoy_projectile",
            "flashbang_projectile"
        ];
        private readonly List<(string Model, float Scale)> _defaultExplosiveModels = [
            ("models/food/fruits/banana01a.vmdl", 1.0f),
            ("models/food/fruits/watermelon01a.vmdl", 1.0f),
            ("models/food/vegetables/cabbage01a.vmdl", 1.0f),
            ("models/food/vegetables/onion01a.vmdl", 1.0f),
            ("models/food/vegetables/pepper01a.vmdl", 1.0f),
            ("models/food/vegetables/potato01a.vmdl", 1.0f),
            ("models/food/vegetables/zucchini01a.vmdl", 1.0f),
        ];

        private Dictionary<string, string> DiceNoExplosives(CCSPlayerController player, CCSPlayerPawn playerPawn)
        {
            // register listener
            if (_playersWithoutExplosives.Count == 0)
            {
                RegisterListener<Listeners.OnEntitySpawned>(DiceNoExplosivesOnEntitySpawned);
            }

            _playersWithoutExplosives.Add(playerPawn);
            return new Dictionary<string, string>
            {
                { "playerName", player.PlayerName }
            };
        }

        private void DiceNoExplosivesOnEntitySpawned(CEntityInstance entity)
        {
            if (_playersWithoutExplosives.Count == 0)
            {
                return;
            }
            // check if entity is a grenade projectile
            if (_grenadeProjectiles.Contains(entity.DesignerName))
            {
                DiceNoExplosivesHandle(entity.Handle);
            }
        }

        private void DiceNoExplosivesHandle(nint handle)
        {
            //
            Dictionary<string, object> config = GetDiceConfig("DiceNoExplosives");
            _ = AddTimer(Convert.ToSingle(config["swap_delay"]), () =>
            {
                if (handle == IntPtr.Zero)
                {
                    return;
                }

                CBaseGrenade grenade = new(handle);
                if (!grenade.IsValid || grenade.Handle == IntPtr.Zero || grenade.AbsOrigin == null)
                {
                    return;
                }

                CBaseEntity? owner = grenade.OwnerEntity?.Value;
                if (owner == null
                    || !owner.IsValid)
                {
                    return;
                }

                if (_playersWithoutExplosives.Contains(owner))
                {
                    // get all models from config
                    List<(string Model, float Scale)> models = [.. ((List<object>)config["models"])
                        .Select(m =>
                        {
                            Dictionary<string, object> dict = (Dictionary<string, object>)m;
                            return (Model: (string)dict["Model"], Scale: (float)Convert.ToSingle(dict["Scale"]));
                        })];
                    if (models.Count == 0)
                    {
                        DebugPrint($"DiceNoExplosives: No models found in config.");
                        return;
                    }

                    (string Model, float Scale) = models[new Random().Next(models.Count)];
                    // create physics model
                    _ = CreatePhysicsModel(
                        Model,
                        Scale,
                        grenade.AbsOrigin,
                        new QAngle(0, 0, 0),
                        new Vector(
                            grenade.Velocity.X,
                            grenade.Velocity.Y,
                            grenade.Velocity.Z
                        )
                    );
                    // stop sound and kill grenade
                    _ = grenade.EmitSound("StopSoundEvents.StopAllExceptMusic");
                    grenade.AcceptInput("Kill");
                }
            });
        }

        private static uint CreatePhysicsModel(string model, float scale, Vector origin, QAngle angles, Vector velocity)
        {
            // create physics prop
            CDynamicProp prop;
            prop = Utilities.CreateEntityByName<CDynamicProp>("prop_physics_override")!;
            // settings
            prop.Health = 10;
            prop.MaxHealth = 10;
            // spawn it
            prop.DispatchSpawn();
            // set model and scale
            prop.SetModel(model);
            prop.CBodyComponent!.SceneNode!.Scale = scale;
            // teleport properly
            prop.Teleport(origin, angles, velocity);
            return prop.Index;
        }
    }
}
