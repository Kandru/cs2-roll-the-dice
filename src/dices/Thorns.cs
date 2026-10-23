using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Configs;
using RollTheDice.Utils;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    public class Thorns : DiceBlueprint
    {
        public override string ClassName => "Thorns";
        public override List<string> Listeners { get; } = ["OnPlayerTakeDamagePre"];

        public Thorns(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        /// <summary>
        /// A share of the damage the holder takes goes back to the attacker. Dice damage is skipped (marker), so two
        /// holders cannot bounce damage back and forth.
        /// </summary>
        public HookResult OnPlayerTakeDamagePre(CCSPlayerPawn pawn, CTakeDamageInfo info)
        {
            if (_players.Count == 0
                || Damage.IsFromDice(info)
                || info.Damage <= 0f
                || HolderOf(pawn) is not { } holder
                || info.Attacker.Value is not { IsValid: true } attackerEntity
                || attackerEntity.Index == pawn.Index
                || attackerEntity.DesignerName != "player")
            {
                return HookResult.Continue;
            }

            CCSPlayerPawn attacker = attackerEntity.As<CCSPlayerPawn>();
            if (attacker.Controller.Value?.As<CCSPlayerController>() is not { IsValid: true } attackerPlayer
                || !Targets.IsEnemy(holder, attackerPlayer))
            {
                return HookResult.Continue;
            }

            float share = info.Damage * _config.Dices.Thorns.ReflectShare;
            // after this damage is through: TakeDamage inside a damage hook would nest
            Server.NextFrame(() => Damage.Deal(attacker, pawn.IsValid ? pawn : null, share));
            return HookResult.Continue;
        }
    }
}

namespace RollTheDice.Configs
{
    public class ThornsConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        // share of the damage taken that hits the attacker (0.0 - 1.0)
        [JsonPropertyName("reflect_share")] public float ReflectShare { get; set; } = 0.3f;
    }
}
