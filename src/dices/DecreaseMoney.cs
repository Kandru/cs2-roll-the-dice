using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;

namespace RollTheDice.Dices
{
    public class DecreaseMoney : ParentDice
    {
        public override string ClassName => "DecreaseMoney";
        public readonly Random _random = new();

        public DecreaseMoney(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
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
            // get random amount of money to decrease
            int moneyDecrease = _random.Next(
                _config.Dices.DecreaseMoney.MinMoney,
                _config.Dices.DecreaseMoney.MaxMoney + 1
            );
            // avoid negative money
            if (player.InGameMoneyServices.Account - moneyDecrease < 0)
            {
                moneyDecrease = player.InGameMoneyServices.Account;
            }
            // decrease money
            player.InGameMoneyServices.Account -= moneyDecrease;
            Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInGameMoneyServices");
            // create GUI for player
            Dictionary<string, string> data = new()
            {
                { "playerName", player.PlayerName },
                { "money", moneyDecrease.ToString() }
            };
            _players.Add(player, new Dictionary<string, CPointWorldText?>
            {
                { "gui", CreateMainGUI(player, ClassName, data) }
            });
        }
    }
}
