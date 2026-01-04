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
            "OnEntitySpawned",
            "OnEntityTakeDamagePre"
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
        private readonly Dictionary<nint, CCSPlayerController> _grenadesThrownByPlayers = [];

        public NoExplosives(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Reset()
        {
            _players.Clear();
            _grenadesThrownByPlayers.Clear();
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

        public HookResult OnEntityTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            if (entity is null
                || !entity.IsValid
                || info.Inflictor is null
                || !info.Inflictor.IsValid
                || info.Inflictor.Value is null
                || !info.Inflictor.Value.IsValid
                // check if grenade was thrown by player
                || !_grenadesThrownByPlayers.ContainsKey(info.Inflictor.Value.Handle))
            {
                return HookResult.Continue;
            }
            nint inflictorHandler = info.Inflictor.Value.Handle;
            // change to proper attacker (otherwise counts as self-kill)
            info.Attacker.Raw = _grenadesThrownByPlayers[inflictorHandler].Pawn.Raw;
            // change to headshot (looks better in the killfeed)
            info.BitsDamageType = DamageTypes_t.DMG_HEADSHOT;
            // update the result
            return HookResult.Changed;
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
                    nint inflictorHandler = CreatePhysicsModel(
                        Model,
                        _config.Dices.NoExplosives.ModelScale,
                        grenade.AbsOrigin,
                        new QAngle(0, 0, 0),
                        new Vector(
                            grenade.Velocity.X,
                            grenade.Velocity.Y,
                            grenade.Velocity.Z
                        ));
                    // create physics model and add to list
                    _grenadesThrownByPlayers.Add(
                        inflictorHandler,
                        owner.Controller.Value.As<CCSPlayerController>());
                    // remove entry after some time, but delay because we wait for eventual damage
                    _ = new CounterStrikeSharp.API.Modules.Timers.Timer(10f, () =>
                    {
                        _grenadesThrownByPlayers.Remove(inflictorHandler);
                    });
                    // stop sound and kill grenade
                    _ = grenade.EmitSound("StopSoundEvents.StopAllExceptMusic");
                    // kill original grenade
                    grenade.AcceptInput("Kill");
                }
            });
        }

        private static nint CreatePhysicsModel(string model, float scale, Vector origin, QAngle angles, Vector velocity)
        {
            // create physics prop
            CPhysicsProp prop;
            prop = Utilities.CreateEntityByName<CPhysicsProp>("prop_physics_override")!;
            // settings
            prop.Health = 10;
            prop.MaxHealth = 10;
            // scale
            CEntityKeyValues kv = new();
            kv.SetFloat("modelscale", 5f);
            // spawn it
            prop.DispatchSpawn(kv);
            // set model
            prop.SetModel(model);
            // teleport properly
            prop.Teleport(origin, angles, velocity);
            return prop.Handle;
        }
    }
}
