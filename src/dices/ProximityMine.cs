using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Configs;
using RollTheDice.Utils;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    public class ProximityMine : DiceBlueprint
    {
        public override string ClassName => "ProximityMine";
        public override List<string> Listeners { get; } = ["OnEntitySpawned", "OnTick"];
        // the fuse is pushed this far out; the mine goes off by itself after its lifetime anyway
        private const float FuseSeconds = 3600f;

        private sealed class Mine(CHEGrenadeProjectile grenade, CCSPlayerController owner, float until)
        {
            public readonly CHEGrenadeProjectile Grenade = grenade;
            public readonly CCSPlayerController Owner = owner;
            public readonly float Until = until;
        }

        private readonly List<Mine> _mines = [];

        public ProximityMine(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            base.Add(player);
            if (_players.Contains(player))
            {
                Weapons.GiveOnce(player, "weapon_hegrenade");
            }
        }

        public override void Reset()
        {
            base.Reset();
            _mines.Clear();
        }

        /// <summary>An HE of the holder becomes a mine: its fuse is pushed out, the oldest mine over the limit blows.</summary>
        public void OnEntitySpawned(CEntityInstance entity)
        {
            if (_players.Count == 0 || entity.DesignerName != "hegrenade_projectile")
            {
                return;
            }
            CHEGrenadeProjectile grenade = entity.As<CHEGrenadeProjectile>();
            Server.NextFrame(() =>
            {
                if (Grenades.ThrowerOf(grenade) is not { } owner || !_players.Contains(owner))
                {
                    return;
                }
                ProximityMineConfig config = _config.Dices.ProximityMine;
                float now = Server.CurrentTime;
                grenade.DetonateTime = now + FuseSeconds;
                _mines.Add(new Mine(grenade, owner, now + config.LifetimeSeconds));

                List<Mine> own = [.. _mines.Where(m => m.Owner == owner && m.Grenade.IsValid)];
                if (own.Count > config.MaxMines)
                {
                    Blow(own[0]);
                }
                Debug($"{owner.PlayerName} placed a mine ({own.Count})");
            });
        }

        /// <summary>8 times per second: a resting mine blows when an enemy comes close.</summary>
        public void OnTick()
        {
            if (_mines.Count == 0 || Server.TickCount % 8 != 0)
            {
                return;
            }

            ProximityMineConfig config = _config.Dices.ProximityMine;
            float now = Server.CurrentTime;
            for (int i = _mines.Count - 1; i >= 0; i--)
            {
                Mine mine = _mines[i];
                if (!mine.Grenade.IsValid)
                {
                    _mines.RemoveAt(i);
                    continue;
                }
                if (now >= mine.Until)
                {
                    Blow(mine);
                    continue;
                }
                Vector v = Motion.Velocity(mine.Grenade);
                if (mine.Grenade.AbsOrigin is not { } position || (v.X * v.X) + (v.Y * v.Y) + (v.Z * v.Z) > 25f)
                {
                    // still flying or rolling: not armed yet
                    continue;
                }
                if (mine.Owner.IsValid && EnemyNear(mine.Owner, position, config.TriggerRadius))
                {
                    Blow(mine);
                }
            }
        }

        private static bool EnemyNear(CCSPlayerController owner, Vector position, float radius)
        {
            foreach ((CCSPlayerController player, CCSPlayerPawn _, float _) in Targets.Around(position, radius))
            {
                if (Targets.IsEnemy(owner, player))
                {
                    return true;
                }
            }
            return false;
        }

        private void Blow(Mine mine)
        {
            _ = _mines.Remove(mine);
            if (mine.Grenade.IsValid)
            {
                mine.Grenade.DetonateTime = Server.CurrentTime;
            }
        }
    }
}

namespace RollTheDice.Configs
{
    public class ProximityMineConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        // an enemy this close to a resting mine sets it off
        [JsonPropertyName("trigger_radius")] public float TriggerRadius { get; set; } = 120f;
        // mines per player at once; a further one sets off the oldest
        [JsonPropertyName("max_mines")] public int MaxMines { get; set; } = 2;
        [JsonPropertyName("lifetime_seconds")] public float LifetimeSeconds { get; set; } = 60f;
    }
}
