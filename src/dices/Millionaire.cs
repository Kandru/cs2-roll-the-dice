using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;

namespace RollTheDice.Dices
{
    public class Millionaire : DiceBlueprint
    {
        public override string ClassName => "Millionaire";

        public Millionaire(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            // check if player is valid and has a pawn
            if (player == null
                || !player.IsValid
                || player.Pawn?.Value == null
                || !player.Pawn.Value.IsValid
                || player.InGameMoneyServices == null)
            {
                return;
            }
            _players.Add(player);
            player.InGameMoneyServices.Account = _config.Dices.Millionaire.Money;
            Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInGameMoneyServices");
            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName }
            });
        }
    }
}
