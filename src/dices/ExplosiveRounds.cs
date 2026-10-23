using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Configs;
using RollTheDice.Utils;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    public class ExplosiveRounds : DiceBlueprint
    {
        public override string ClassName => "ExplosiveRounds";
        public override List<string> Events { get; } = ["EventBulletImpact"];
        // time of the holder's last blast: at most one per interval (a Negev would spawn 15 per second)
        private readonly Dictionary<CCSPlayerController, float> _lastBlast = [];
        private const float SurfaceGap = 8f;
        private const float BodyHeight = 32f;
        private const float ParticleSeconds = 3f;

        public ExplosiveRounds(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            base.Remove(player, reason);
            _ = _lastBlast.Remove(player);
        }

        public override void Reset()
        {
            base.Reset();
            _lastBlast.Clear();
        }

        /// <summary>
        /// Bullets of the holder blow up where they hit: a small fireball and a few points of damage to enemies near it
        /// (the shooter is spared unless the config says otherwise). The damage is dealt here and not by an env_explosion,
        /// whose blast starts inside the wall the bullet hit and did not reach anyone.
        /// </summary>
        public HookResult EventBulletImpact(EventBulletImpact @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (_players.Count == 0
                || player is not { IsValid: true }
                || !_players.Contains(player)
                || player.PlayerPawn.Value is not { IsValid: true } pawn
                || pawn.WeaponServices?.ActiveWeapon.Value is not { IsValid: true } weapon
                || !Weapons.IsGun(weapon.DesignerName))
            {
                return HookResult.Continue;
            }

            ExplosiveRoundsConfig config = _config.Dices.ExplosiveRounds;
            float now = Server.CurrentTime;
            if (_lastBlast.TryGetValue(player, out float last) && now - last < config.IntervalSeconds)
            {
                return HookResult.Continue;
            }
            _lastBlast[player] = now;
            // a little out of the surface towards the shooter, so the lines to the victims do not start in the wall
            Vector impact = new(@event.X, @event.Y, @event.Z);
            Vector eyes = Targets.Eyes(pawn);
            float length = Vectors.GetDistance(impact, eyes);
            Vector blast = length > SurfaceGap
                ? new Vector(impact.X + ((eyes.X - impact.X) / length * SurfaceGap), impact.Y + ((eyes.Y - impact.Y) / length * SurfaceGap), impact.Z + ((eyes.Z - impact.Z) / length * SurfaceGap))
                : impact;
            Particle.Start(Explosion.SmallParticleEffect, blast, ParticleSeconds);
            Server.NextFrame(() => Blast(player, blast, config));
            return HookResult.Continue;
        }

        private void Blast(CCSPlayerController shooter, Vector blast, ExplosiveRoundsConfig config)
        {
            CCSPlayerPawn? attacker = shooter.IsValid && shooter.PlayerPawn.Value is { IsValid: true } p ? p : null;
            // Around measures to the feet; the body middle can be this much further away
            foreach ((CCSPlayerController player, CCSPlayerPawn pawn, float _) in Targets.Around(blast, config.Radius + BodyHeight))
            {
                bool self = player == shooter;
                if ((!self && !Targets.IsEnemy(shooter, player)) || (self && !config.HurtsShooter) || pawn.AbsOrigin is not { } origin)
                {
                    continue;
                }
                Vector body = new(origin.X, origin.Y, origin.Z + BodyHeight);
                float distance = Vectors.GetDistance(blast, body);
                if (distance > config.Radius)
                {
                    continue;
                }
                TraceResult trace = RayTrace.TraceLine(blast, body, pawn);
                if (trace.DidHit() && trace.Fraction < 0.97f)
                {
                    continue;
                }
                float damage = config.Damage - ((config.Damage - config.MinDamage) * (distance / config.Radius));
                int before = pawn.Health;
                Damage.Deal(pawn, attacker, damage, DamageTypes_t.DMG_BLAST);
                Debug($"{shooter.PlayerName} blast hits {player.PlayerName} at {distance:0} ({before} -> {pawn.Health} HP)");
            }
        }
    }
}

namespace RollTheDice.Configs
{
    public class ExplosiveRoundsConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        // damage at the middle of the blast, falling to min_damage at the edge of the radius
        [JsonPropertyName("damage")] public float Damage { get; set; } = 4f;
        [JsonPropertyName("min_damage")] public float MinDamage { get; set; } = 3f;
        [JsonPropertyName("radius")] public float Radius { get; set; } = 100f;
        // at most one blast per player in this time
        [JsonPropertyName("interval_seconds")] public float IntervalSeconds { get; set; } = 0.25f;
        // blasts at your own feet hurt you too
        [JsonPropertyName("hurts_shooter")] public bool HurtsShooter { get; set; } = false;
    }
}
