using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace RollTheDice
{
    public partial class RollTheDice : BasePlugin
    {
        private Dictionary<string, string> DiceDecreaseMoney(CCSPlayerController player, CCSPlayerPawn playerPawn)
        {
            Dictionary<string, object> config = GetDiceConfig("DiceDecreaseMoney");
            // decrease money randomly
            var moneyDecrease = _random.Next(
                Convert.ToInt32(config["min_money"]),
                Convert.ToInt32(config["max_money"]) + 1
            );
            // bounds check
            if (player.InGameMoneyServices!.Account - moneyDecrease >= 0)
            {
                player.InGameMoneyServices!.Account -= moneyDecrease;
            }
            else
            {
                player.InGameMoneyServices!.Account = 0;
            }
            // update player state
            Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInGameMoneyServices");
            return new Dictionary<string, string>
            {
                { "playerName", player.PlayerName },
                { "money", moneyDecrease.ToString() }
            };
        }

        private Dictionary<string, object> DiceDecreaseMoneyConfig()
        {
            var config = new Dictionary<string, object>();
            config["min_money"] = (int)100;
            config["max_money"] = (int)1000;
            return config;
        }
    }
}
