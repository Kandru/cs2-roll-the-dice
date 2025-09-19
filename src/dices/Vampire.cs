using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;

namespace RollTheDice.Dices
{
    public class Vampire : DiceBlueprint
    {
        public override string ClassName => "Vampire";
        public override List<string> Events => [
            "EventPlayerDeath",
            "EventPlayerHurt"
        ];
        public readonly Random _random = new();
        public readonly Dictionary<CCSPlayerController, float> _playerSpeed = [];

        public Vampire(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (player == null
                || !_players.Contains(player)
                || player.PlayerPawn?.Value?.WeaponServices == null)
            {
                return HookResult.Continue;
            }
            Remove(player);
            return HookResult.Continue;
        }

        public HookResult EventPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
        {
            CCSPlayerController? attacker = @event.Attacker;
            CCSPlayerController? victim = @event.Userid;
            if (attacker == null
                || victim == null
                || !_players.Contains(attacker)
                || attacker.Pawn?.Value == null)
            {
                return HookResult.Continue;
            }

            attacker.Pawn.Value.Health += (int)float.Round(@event.DmgHealth);
            if (attacker.Pawn.Value.Health > _config.Dices.Vampire.MaxHealth)
            {
                attacker.Pawn.Value.Health = _config.Dices.Vampire.MaxHealth;
            }
            attacker.Pawn.Value.MaxHealth = attacker.Pawn.Value.Health;
            Utilities.SetStateChanged(attacker.Pawn.Value, "CBaseEntity", "m_iHealth");
            Utilities.SetStateChanged(attacker.Pawn.Value, "CBaseEntity", "m_iMaxHealth");
            attacker.PrintToCenterAlert($"+{(int)float.Round(@event.DmgHealth)} health!");
            return HookResult.Continue;
        }
    }
}
