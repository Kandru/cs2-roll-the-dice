using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Configs;
using RollTheDice.Utils;
using System.Drawing;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    public class IncendiaryRounds : HitEffect
    {
        public override string ClassName => "IncendiaryRounds";
        protected override float Seconds => _config.Dices.IncendiaryRounds.BurnSeconds;
        protected override Color Color => Color.FromArgb(255, 255, 120, 40);

        public IncendiaryRounds(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        /// <summary>Burn damage in steps; the shooter gets the kill (fire icon in the kill feed).</summary>
        protected override void Apply(CCSPlayerController victim, CCSPlayerPawn pawn, Afflicted effect, float now)
        {
            IncendiaryRoundsConfig config = _config.Dices.IncendiaryRounds;
            if (now < effect.NextAt)
            {
                return;
            }
            effect.NextAt = now + config.TickSeconds;
            CCSPlayerPawn? attacker = effect.Shooter.IsValid ? effect.Shooter.PlayerPawn.Value : null;
            Damage.Deal(pawn, attacker, config.DamagePerTick, DamageTypes_t.DMG_BURN);
        }
    }
}

namespace RollTheDice.Configs
{
    public class IncendiaryRoundsConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        // a new hit starts the time again (no stacking)
        [JsonPropertyName("burn_seconds")] public float BurnSeconds { get; set; } = 3f;
        [JsonPropertyName("damage_per_tick")] public float DamagePerTick { get; set; } = 4f;
        [JsonPropertyName("tick_seconds")] public float TickSeconds { get; set; } = 0.5f;
    }
}
