using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace RollTheDice
{
    public partial class RollTheDice : BasePlugin
    {
        private List<CCSPlayerPawn> _playersWithoutExplosives = new();
        private readonly List<(string Model, float Scale)> _explosiveModels = new() {
            ("models/food/fruits/banana01a.vmdl", 1.0f),
            ("models/food/fruits/watermelon01a.vmdl", 1.0f),
            ("models/food/vegetables/cabbage01a.vmdl", 1.0f),
            ("models/food/vegetables/onion01a.vmdl", 1.0f),
            ("models/food/vegetables/pepper01a.vmdl", 1.0f),
            ("models/food/vegetables/potato01a.vmdl", 1.0f),
            ("models/food/vegetables/zucchini01a.vmdl", 1.0f),
        };

        private Dictionary<string, string> DiceNoExplosives(CCSPlayerController player, CCSPlayerPawn playerPawn)
        {
            // register listener
            if (_playersWithoutExplosives.Count == 0) RegisterListener<Listeners.OnEntitySpawned>(DiceNoExplosivesOnEntitySpawned);
            _playersWithoutExplosives.Add(playerPawn);
            return new Dictionary<string, string>
            {
                { "playerName", player.PlayerName }
            };
        }

        private void DiceNoExplosivesReset()
        {
            RemoveListener<Listeners.OnEntitySpawned>(DiceNoExplosivesOnEntitySpawned);
            _playersWithoutExplosives.Clear();
        }

        private void DiceNoExplosivesResetForPlayer(CCSPlayerController player)
        {
            if (player.PlayerPawn == null
                || !player.PlayerPawn.IsValid
                || player.PlayerPawn.Value == null) return;
            if (!_playersWithoutExplosives.Contains(player.PlayerPawn.Value)) return;
            _playersWithoutExplosives.Remove(player.PlayerPawn.Value);
            // remove listener if no players have this dice
            if (_playersWithoutExplosives.Count == 0) DiceNoExplosivesReset();
        }

        private void DiceNoExplosivesUnload()
        {
            DiceNoExplosivesReset();
        }

        private void DiceNoExplosivesOnEntitySpawned(CEntityInstance entity)
        {
            // remove listener if no players to save resources
            if (_playersWithoutExplosives.Count == 0) return;
            // handle smoke grenades
            if (entity.DesignerName == "smokegrenade_projectile") DiceNoExplosivesHandleSmokeGrenade(entity.Handle);
            // handle HE grenades
            if (entity.DesignerName == "hegrenade_projectile") DiceNoExplosivesHandleHEGrenade(entity.Handle);
            // handle molotovs
            if (entity.DesignerName == "molotov_projectile") DiceNoExplosivesHandleMolotov(entity.Handle);
            // handle decoy grenades
            if (entity.DesignerName == "decoy_projectile") DiceNoExplosivesHandleDecoy(entity.Handle);
            // handle flashbang
            if (entity.DesignerName == "flashbang_projectile") DiceNoExplosivesHandleFlashbang(entity.Handle);
        }

        private void DiceNoExplosivesHandleSmokeGrenade(nint handle)
        {
            Dictionary<string, object> config = GetDiceConfig("DiceNoExplosives");
            AddTimer(Convert.ToSingle(config["swap_delay"]), () =>
            {
                CSmokeGrenadeProjectile grenade = new(handle);
                if (!grenade.IsValid || grenade.Handle == IntPtr.Zero || grenade.AbsOrigin == null) return;
                CBaseEntity? owner = grenade.OwnerEntity?.Value;
                if (owner == null) return;
                if (_playersWithoutExplosives.Contains(owner))
                {
                    CreatePhysicsModel(
                        grenade.AbsOrigin,
                        new QAngle(0, 0, 0),
                        new Vector(
                            grenade.Velocity.X,
                            grenade.Velocity.Y,
                            grenade.Velocity.Z
                        )
                    );
                    grenade.AcceptInput("Kill");
                }
            });
        }

        private void DiceNoExplosivesHandleHEGrenade(nint handle)
        {
            Dictionary<string, object> config = GetDiceConfig("DiceNoExplosives");
            AddTimer(Convert.ToSingle(config["swap_delay"]), () =>
            {
                CHEGrenadeProjectile grenade = new(handle);
                if (!grenade.IsValid || grenade.Handle == IntPtr.Zero || grenade.AbsOrigin == null) return;
                CBaseEntity? owner = grenade.OwnerEntity?.Value;
                if (owner == null) return;
                if (_playersWithoutExplosives.Contains(owner))
                {
                    CreatePhysicsModel(
                        grenade.AbsOrigin,
                        new QAngle(0, 0, 0),
                        new Vector(
                            grenade.Velocity.X,
                            grenade.Velocity.Y,
                            grenade.Velocity.Z
                        )
                    );
                    grenade.AcceptInput("Kill");
                }
            });
        }

        private void DiceNoExplosivesHandleMolotov(nint handle)
        {
            Dictionary<string, object> config = GetDiceConfig("DiceNoExplosives");
            AddTimer(Convert.ToSingle(config["swap_delay"]), () =>
            {
                CMolotovProjectile grenade = new(handle);
                if (!grenade.IsValid || grenade.Handle == IntPtr.Zero || grenade.AbsOrigin == null) return;
                CBaseEntity? owner = grenade.OwnerEntity?.Value;
                if (owner == null) return;
                if (_playersWithoutExplosives.Contains(owner))
                {
                    CreatePhysicsModel(
                        grenade.AbsOrigin,
                        new QAngle(0, 0, 0),
                        new Vector(
                            grenade.Velocity.X,
                            grenade.Velocity.Y,
                            grenade.Velocity.Z
                        )
                    );
                    grenade.AcceptInput("Kill");
                }
            });
        }

        private void DiceNoExplosivesHandleDecoy(nint handle)
        {
            Dictionary<string, object> config = GetDiceConfig("DiceNoExplosives");
            AddTimer(Convert.ToSingle(config["swap_delay"]), () =>
            {
                CDecoyProjectile grenade = new(handle);
                if (!grenade.IsValid || grenade.Handle == IntPtr.Zero || grenade.AbsOrigin == null) return;
                CBaseEntity? owner = grenade.OwnerEntity?.Value;
                if (owner == null) return;
                if (_playersWithoutExplosives.Contains(owner))
                {
                    CreatePhysicsModel(
                        grenade.AbsOrigin,
                        new QAngle(0, 0, 0),
                        new Vector(
                            grenade.Velocity.X,
                            grenade.Velocity.Y,
                            grenade.Velocity.Z
                        )
                    );
                    grenade.AcceptInput("Kill");
                }
            });
        }

        private void DiceNoExplosivesHandleFlashbang(nint handle)
        {
            Dictionary<string, object> config = GetDiceConfig("DiceNoExplosives");
            AddTimer(Convert.ToSingle(config["swap_delay"]), () =>
            {
                CFlashbangProjectile grenade = new(handle);
                if (!grenade.IsValid || grenade.Handle == IntPtr.Zero || grenade.AbsOrigin == null) return;
                CBaseEntity? owner = grenade.OwnerEntity?.Value;
                if (owner == null) return;
                if (_playersWithoutExplosives.Contains(owner))
                {
                    CreatePhysicsModel(
                        grenade.AbsOrigin,
                        new QAngle(0, 0, 0),
                        new Vector(
                            grenade.Velocity.X,
                            grenade.Velocity.Y,
                            grenade.Velocity.Z
                        )
                    );
                    grenade.AcceptInput("Kill");
                }
            });
        }

        private uint CreatePhysicsModel(Vector origin, QAngle angles, Vector velocity)
        {
            // create physics prop
            CDynamicProp prop;
            prop = Utilities.CreateEntityByName<CDynamicProp>("prop_physics_override")!;
            // settings
            prop.Health = 10;
            prop.MaxHealth = 10;
            // spawn it
            prop.DispatchSpawn();
            var randomModel = _explosiveModels[new Random().Next(_explosiveModels.Count)];
            prop.SetModel(randomModel.Model);
            prop.CBodyComponent!.SceneNode!.Scale = randomModel.Scale;
            prop.Teleport(origin, angles, velocity);
            prop.AnimGraphUpdateEnabled = false;
            return prop.Index;
        }

        private Dictionary<string, object> DiceNoExplosivesConfig()
        {
            var config = new Dictionary<string, object>();
            config["swap_delay"] = (float)0.1f;
            return config;
        }
    }
}
