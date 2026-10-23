using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Configs;
using RollTheDice.Utils;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    public class Bouncy : DiceBlueprint
    {
        public override string ClassName => "Bouncy";
        public override List<string> Listeners { get; } = ["OnTick", "OnPlayerTakeDamagePre"];
        private readonly LandingTracker _landing = new();

        public Bouncy(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            base.Remove(player, reason);
            _landing.Forget(player);
        }

        public override void Reset()
        {
            base.Reset();
            _landing.Clear();
        }

        /// <summary>A hard landing springs back up; holding duck lands normally.</summary>
        public void OnTick()
        {
            if (_players.Count == 0)
            {
                return;
            }

            BouncyConfig config = _config.Dices.Bouncy;
            foreach (CCSPlayerController player in _players)
            {
                if (!IsAlivePlayer(player)
                    || player.PlayerPawn.Value is not { IsValid: true } pawn
                    || !_landing.Landed(player, pawn, out float impact)
                    || impact < config.MinImpactSpeed
                    || (config.DuckStopsBounce && (player.Buttons & PlayerButtons.Duck) != 0))
                {
                    continue;
                }
                Motion.SetVerticalVelocity(pawn, Math.Min(impact * config.BounceFactor, config.MaxBounceSpeed));
                Debug($"{player.PlayerName} bounces, impact {impact:0}");
            }
        }

        public HookResult OnPlayerTakeDamagePre(CCSPlayerPawn pawn, CTakeDamageInfo info)
        {
            return _players.Count > 0
                && (info.BitsDamageType & DamageTypes_t.DMG_FALL) != 0
                && HolderOf(pawn) != null
                ? HookResult.Stop
                : HookResult.Continue;
        }
    }
}

namespace RollTheDice.Configs
{
    public class BouncyConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        // falling speed from which a landing bounces (a normal jump lands with about 300, a drop off a crate with 320)
        [JsonPropertyName("min_impact_speed")] public float MinImpactSpeed { get; set; } = 320f;
        // share of the falling speed kept per bounce: 0.92 bounces a long drop five or six times
        [JsonPropertyName("bounce_factor")] public float BounceFactor { get; set; } = 0.92f;
        [JsonPropertyName("max_bounce_speed")] public float MaxBounceSpeed { get; set; } = 1100f;
        [JsonPropertyName("duck_stops_bounce")] public bool DuckStopsBounce { get; set; } = true;
    }
}
