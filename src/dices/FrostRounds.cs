using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using System.Drawing;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    public class FrostRounds : HitEffect
    {
        public override string ClassName => "FrostRounds";
        protected override float Seconds => _config.Dices.FrostRounds.SlowSeconds;
        protected override Color Color => Color.FromArgb(255, 110, 180, 255);

        public FrostRounds(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        /// <summary>The engine resets the velocity modifier after every hit, so it is written each tick.</summary>
        protected override void Apply(CCSPlayerController victim, CCSPlayerPawn pawn, Afflicted effect, float now)
        {
            float speed = _config.Dices.FrostRounds.SpeedFactor;
            if (pawn.VelocityModifier > speed)
            {
                pawn.VelocityModifier = speed;
                Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
            }
        }

        protected override void End(CCSPlayerController victim)
        {
            if (victim.IsValid && victim.PlayerPawn.Value is { IsValid: true } pawn && pawn.VelocityModifier < 1f)
            {
                pawn.VelocityModifier = 1f;
                Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
            }
        }
    }
}

namespace RollTheDice.Configs
{
    public class FrostRoundsConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        // a new hit starts the time again (no stacking)
        [JsonPropertyName("slow_seconds")] public float SlowSeconds { get; set; } = 1.5f;
        // share of the normal running speed while frozen
        [JsonPropertyName("speed_factor")] public float SpeedFactor { get; set; } = 0.4f;
    }
}
