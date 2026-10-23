using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Configs;
using RollTheDice.Utils;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    public class KnockbackRounds : DiceBlueprint
    {
        public override string ClassName => "KnockbackRounds";
        public override List<string> Events { get; } = ["EventPlayerHurt"];

        public KnockbackRounds(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        /// <summary>A bullet hit pushes the enemy away from the shooter, harder the more damage it did.</summary>
        public HookResult EventPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
        {
            CCSPlayerController? shooter = @event.Attacker;
            CCSPlayerController? victim = @event.Userid;
            if (_players.Count == 0
                || Damage.IsDealing
                || shooter is not { IsValid: true }
                || !_players.Contains(shooter)
                || victim is not { IsValid: true }
                || !Targets.IsEnemy(shooter, victim)
                || !Weapons.IsGun(@event.Weapon)
                || !Targets.IsAlive(victim)
                || shooter.PlayerPawn.Value is not { IsValid: true, AbsOrigin: { } from }
                || victim.PlayerPawn.Value is not { IsValid: true, AbsOrigin: { } to } pawn)
            {
                return HookResult.Continue;
            }

            KnockbackRoundsConfig config = _config.Dices.KnockbackRounds;
            float dx = to.X - from.X;
            float dy = to.Y - from.Y;
            float length = MathF.Sqrt((dx * dx) + (dy * dy));
            if (length < 1f)
            {
                return HookResult.Continue;
            }
            float push = Math.Min(config.MaxPush, @event.DmgHealth * config.PushPerDamage);
            Vector v = Motion.Velocity(pawn);
            v.X += dx / length * push;
            v.Y += dy / length * push;
            v.Z = Math.Max(v.Z, config.Lift);
            Server.NextFrame(() =>
            {
                if (pawn.IsValid)
                {
                    Motion.SetVelocity(pawn, v);
                }
            });
            return HookResult.Continue;
        }
    }
}

namespace RollTheDice.Configs
{
    public class KnockbackRoundsConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        // push in units/s per point of damage, capped
        [JsonPropertyName("push_per_damage")] public float PushPerDamage { get; set; } = 12f;
        [JsonPropertyName("max_push")] public float MaxPush { get; set; } = 700f;
        // upward share, lifts the victim off the ground so the push is not eaten by friction
        [JsonPropertyName("lift")] public float Lift { get; set; } = 180f;
    }
}
