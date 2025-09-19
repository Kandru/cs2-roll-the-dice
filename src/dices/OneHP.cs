using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;

namespace RollTheDice.Dices
{
    public class OneHP : DiceBlueprint
    {
        public override string ClassName => "OneHP";
        public readonly Random _random = new();

        public OneHP(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            // check if player is valid and has a pawn
            if (player == null
                || !player.IsValid
                || player.Pawn?.Value == null
                || !player.Pawn.Value.IsValid)
            {
                return;
            }
            // set player health to 1
            ChangeHealth(player, 1);
            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName }
            });
        }

        private static void ChangeHealth(CCSPlayerController player, int health)
        {
            if (player.Pawn?.Value == null
                || !player.Pawn.Value.IsValid)
            {
                return;
            }
            player.Pawn.Value.Health = health;
            player.Pawn.Value.MaxHealth = health;
            Utilities.SetStateChanged(player.Pawn.Value, "CBaseEntity", "m_iHealth");
            Utilities.SetStateChanged(player.Pawn.Value, "CBaseEntity", "m_iMaxHealth");
        }
    }
}
