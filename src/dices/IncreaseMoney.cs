using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;

namespace RollTheDice.Dices
{
    public class IncreaseMoney : ParentDice
    {
        public override string ClassName => "IncreaseMoney";
        public readonly Random _random = new();

        public IncreaseMoney(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
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
            // get random amount of money to increase
            int moneyIncrease = _random.Next(
                _config.Dices.IncreaseMoney.MinMoney,
                _config.Dices.IncreaseMoney.MaxMoney + 1
            );
            // increase money
            player.InGameMoneyServices.Account += moneyIncrease;
            Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInGameMoneyServices");
            // create GUI for player
            Dictionary<string, string> data = new()
            {
                { "playerName", player.PlayerName },
                { "money", moneyIncrease.ToString() }
            };
            _players.Add(player, new Dictionary<string, CPointWorldText?>
            {
                //{ "gui", CreateMainGUI(player, ClassName, data) }
            });
        }
    }
}
