using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Utils;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    public class BloodMagic : DiceBlueprint
    {
        public override string ClassName => "BloodMagic";
        public override List<string> Events { get; } = ["EventWeaponFire"];
        public override List<string> Listeners { get; } = ["OnPlayerTakeDamagePre"];

        public BloodMagic(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        /// <summary>Every gun shot costs health; the cost never kills (stops at 1).</summary>
        public HookResult EventWeaponFire(EventWeaponFire @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (_players.Count == 0
                || player is not { IsValid: true }
                || !_players.Contains(player)
                || !Weapons.IsGun(@event.Weapon)
                || player.PlayerPawn.Value is not { IsValid: true } pawn
                || pawn.Health <= 1)
            {
                return HookResult.Continue;
            }

            pawn.Health = Math.Max(1, pawn.Health - _config.Dices.BloodMagic.HealthPerShot);
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
            return HookResult.Continue;
        }

        /// <summary>The holder's bullets deal more damage.</summary>
        public HookResult OnPlayerTakeDamagePre(CCSPlayerPawn pawn, CTakeDamageInfo info)
        {
            if (_players.Count == 0
                || Damage.IsFromDice(info)
                || (info.BitsDamageType & DamageTypes_t.DMG_BULLET) == 0
                || info.Attacker.Value is not { } attacker
                || attacker.Index == pawn.Index
                || HolderOf(attacker) == null)
            {
                return HookResult.Continue;
            }

            info.Damage *= _config.Dices.BloodMagic.DamageMultiplier;
            return HookResult.Changed;
        }
    }
}

namespace RollTheDice.Configs
{
    public class BloodMagicConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("health_per_shot")] public int HealthPerShot { get; set; } = 1;
        [JsonPropertyName("damage_multiplier")] public float DamageMultiplier { get; set; } = 1.5f;
    }
}
