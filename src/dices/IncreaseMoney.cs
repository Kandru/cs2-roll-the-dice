using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;

namespace RollTheDice.Dices
{
    public class IncreaseMoney : DiceBlueprint
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
            SetMoney(player, player.InGameMoneyServices.Account + moneyIncrease);
            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName },
                { "money", moneyIncrease.ToString() }
            });
        }

        private static void SetMoney(CCSPlayerController player, int amount)
        {
            if (player == null
                || !player.IsValid
                || player.InGameMoneyServices == null)
            {
                return;
            }
            player.InGameMoneyServices.Account = amount;
            Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInGameMoneyServices");
        }
    }
}
