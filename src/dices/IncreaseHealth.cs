using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;

namespace RollTheDice.Dices
{
    public class IncreaseHealth : ParentDice
    {
        public override string ClassName => "IncreaseHealth";
        public readonly Random _random = new();

        public IncreaseHealth(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
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
            // get random player name
            List<string> PlayerNames = [.. Utilities.GetPlayers()
                .Where(p => p.IsValid && !p.IsBot && !p.IsHLTV && p != player)
                .Select(p => p.PlayerName)];
            // change health
            int healthIncrease = _random.Next(
                _config.Dices.IncreaseHealth.MinHealth,
                _config.Dices.IncreaseHealth.MaxHealth + 1
            );
            // increase health
            player.Pawn.Value.Health += healthIncrease;
            player.Pawn.Value.MaxHealth = player.Pawn.Value.Health;
            Utilities.SetStateChanged(player.Pawn.Value, "CBaseEntity", "m_iHealth");
            Utilities.SetStateChanged(player.Pawn.Value, "CBaseEntity", "m_iMaxHealth");
            // create GUI for player
            Dictionary<string, string> data = new()
            {
                { "playerName", player.PlayerName },
                { "healthIncrease", healthIncrease.ToString() }
            };
            _players.Add(player, new Dictionary<string, CPointWorldText?>
            {
                { "gui", CreateMainGUI(player, ClassName, data) }
            });
        }
    }
}
