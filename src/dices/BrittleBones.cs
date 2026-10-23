using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Configs;
using RollTheDice.Utils;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    public class BrittleBones : DiceBlueprint
    {
        public override string ClassName => "BrittleBones";
        public override List<string> Listeners { get; } = ["OnTick", "OnPlayerTakeDamagePre"];
        // the engine's own fall damage starts at this falling speed ((speed - 580) * 100 / 444, measured in game)
        private const float EngineFallSpeed = 580f;
        private readonly LandingTracker _landing = new();

        public BrittleBones(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
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

        /// <summary>
        /// Small drops the engine does not count as a fall still hurt. The extra hit grows up to the engine's threshold and
        /// stays there, so a higher fall never hurts less than a lower one (the engine's own damage comes on top, x4).
        /// </summary>
        public void OnTick()
        {
            if (_players.Count == 0)
            {
                return;
            }

            BrittleBonesConfig config = _config.Dices.BrittleBones;
            foreach (CCSPlayerController player in _players)
            {
                if (!IsAlivePlayer(player)
                    || player.PlayerPawn.Value is not { IsValid: true } pawn
                    || !_landing.Landed(player, pawn, out float impact)
                    || impact < config.MinImpactSpeed)
                {
                    continue;
                }
                float damage = (Math.Min(impact, EngineFallSpeed) - config.MinImpactSpeed) * config.SmallFallDamagePerSpeed;
                Server.NextFrame(() =>
                {
                    Damage.Deal(pawn, null, damage, DamageTypes_t.DMG_FALL);
                    Debug($"{player.PlayerName} landed at {impact:0}: extra fall damage {damage:0}");
                });
            }
        }

        public HookResult OnPlayerTakeDamagePre(CCSPlayerPawn pawn, CTakeDamageInfo info)
        {
            if (_players.Count == 0
                || Damage.IsFromDice(info)
                || (info.BitsDamageType & DamageTypes_t.DMG_FALL) == 0
                || HolderOf(pawn) == null)
            {
                return HookResult.Continue;
            }
            info.Damage *= _config.Dices.BrittleBones.FallDamageMultiplier;
            return HookResult.Changed;
        }
    }
}

namespace RollTheDice.Configs
{
    public class BrittleBonesConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("fall_damage_multiplier")] public float FallDamageMultiplier { get; set; } = 4f;
        // landings from this falling speed on hurt even when the engine deals no fall damage (a jump lands with about 300)
        [JsonPropertyName("min_impact_speed")] public float MinImpactSpeed { get; set; } = 380f;
        // extra damage per unit of falling speed above min_impact_speed (up to the engine's own fall damage at 580: 20 HP)
        [JsonPropertyName("small_fall_damage_per_speed")] public float SmallFallDamagePerSpeed { get; set; } = 0.1f;
    }
}
