using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Configs;
using RollTheDice.Utils;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    public class HomingGrenades : DiceBlueprint
    {
        public override string ClassName => "HomingGrenades";
        public override List<string> Listeners { get; } = ["OnEntitySpawned", "OnTick"];
        private static readonly HashSet<string> Homing = ["hegrenade_projectile", "molotov_projectile"];
        private readonly List<(CBaseCSGrenadeProjectile Grenade, CCSPlayerController Owner)> _flying = [];

        public HomingGrenades(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
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
            _flying.Clear();
        }

        public void OnEntitySpawned(CEntityInstance entity)
        {
            if (_players.Count == 0 || !Homing.Contains(entity.DesignerName))
            {
                return;
            }
            CBaseCSGrenadeProjectile grenade = entity.As<CBaseCSGrenadeProjectile>();
            Server.NextFrame(() =>
            {
                if (Grenades.ThrowerOf(grenade) is { } owner && _players.Contains(owner))
                {
                    _flying.Add((grenade, owner));
                }
            });
        }

        /// <summary>Every second tick: a flying grenade turns a little towards the nearest enemy in front of it.</summary>
        public void OnTick()
        {
            if (_flying.Count == 0 || Server.TickCount % 2 != 0)
            {
                return;
            }

            HomingGrenadesConfig config = _config.Dices.HomingGrenades;
            float minDot = MathF.Cos(config.ConeDegrees * 0.5f * MathF.PI / 180f);
            for (int i = _flying.Count - 1; i >= 0; i--)
            {
                (CBaseCSGrenadeProjectile grenade, CCSPlayerController owner) = _flying[i];
                if (!grenade.IsValid || !owner.IsValid || grenade.AbsOrigin is not { } position)
                {
                    _flying.RemoveAt(i);
                    continue;
                }

                Vector v = Motion.Velocity(grenade);
                float speed = MathF.Sqrt((v.X * v.X) + (v.Y * v.Y) + (v.Z * v.Z));
                // resting or rolling grenades are left alone
                if (speed < 100f)
                {
                    continue;
                }
                Vector dir = new(v.X / speed, v.Y / speed, v.Z / speed);
                if (Target(owner, grenade, position, dir, minDot, config.Range) is not { } aim)
                {
                    continue;
                }
                float turn = config.TurnRate;
                float x = dir.X + ((aim.X - dir.X) * turn);
                float y = dir.Y + ((aim.Y - dir.Y) * turn);
                float z = dir.Z + ((aim.Z - dir.Z) * turn);
                float length = MathF.Max(0.001f, MathF.Sqrt((x * x) + (y * y) + (z * z)));
                grenade.Teleport(null, null, new Vector(x / length * speed, y / length * speed, z / length * speed));
            }
        }

        /// <summary>Direction to the nearest visible enemy inside the cone, or null.</summary>
        private static Vector? Target(CCSPlayerController owner, CBaseEntity grenade, Vector position, Vector dir, float minDot, float range)
        {
            Vector? best = null;
            float bestDistance = float.MaxValue;
            foreach ((CCSPlayerController player, CCSPlayerPawn pawn, float distance) in Targets.Around(position, range))
            {
                if (distance >= bestDistance || distance < 1f || !Targets.IsEnemy(owner, player) || pawn.AbsOrigin is not { } origin)
                {
                    continue;
                }
                Vector body = new(origin.X, origin.Y, origin.Z + 40f);
                float dx = body.X - position.X;
                float dy = body.Y - position.Y;
                float dz = body.Z - position.Z;
                float length = MathF.Sqrt((dx * dx) + (dy * dy) + (dz * dz));
                Vector to = new(dx / length, dy / length, dz / length);
                if ((to.X * dir.X) + (to.Y * dir.Y) + (to.Z * dir.Z) < minDot)
                {
                    continue;
                }
                TraceResult trace = RayTrace.TraceLine(position, body, grenade);
                if (trace.DidHit() && trace.Fraction < 0.97f && trace.HitEntity()?.Handle != pawn.Handle)
                {
                    continue;
                }
                best = to;
                bestDistance = distance;
            }
            return best;
        }
    }
}

namespace RollTheDice.Configs
{
    public class HomingGrenadesConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        // enemies inside this cone along the flight and within range are homed in on
        [JsonPropertyName("cone_degrees")] public float ConeDegrees { get; set; } = 60f;
        [JsonPropertyName("range")] public float Range { get; set; } = 1200f;
        // share of the turn towards the target per step (every second tick)
        [JsonPropertyName("turn_rate")] public float TurnRate { get; set; } = 0.15f;
    }
}
