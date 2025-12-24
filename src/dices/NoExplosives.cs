using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;

namespace RollTheDice.Dices
{
    public class NoExplosives : DiceBlueprint
    {
        public override string ClassName => "NoExplosives";
        public override List<string> Listeners => [
            "OnEntitySpawned"
        ];
        private readonly HashSet<string> _grenadeProjectiles =
        [
            "smokegrenade_projectile",
            "hegrenade_projectile",
            "molotov_projectile",
            "decoy_projectile",
            "flashbang_projectile"
        ];
        public readonly Random _random = new();

        public NoExplosives(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public void OnEntitySpawned(CEntityInstance entity)
        {
            if (_players.Count == 0)
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
            Server.NextFrame(() =>
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
                CCSPlayerPawn? owner = grenade.OriginalThrower?.Value;
                if (owner == null
                    || !owner.IsValid)
                {
                    return;
                }
                if (owner.Controller?.Value != null && _players.Contains(owner.Controller.Value))
                {
                    if (_config.Dices.NoExplosives.RandomModels.Count == 0)
                    {
                        return;
                    }
                    string Model = _config.Dices.NoExplosives.RandomModels[new Random().Next(_config.Dices.NoExplosives.RandomModels.Count)];
                    // create physics model
                    _ = CreatePhysicsModel(
                        Model,
                        _config.Dices.NoExplosives.ModelScale,
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
