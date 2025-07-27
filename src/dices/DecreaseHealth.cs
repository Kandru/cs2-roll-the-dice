using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class DecreaseHealth : ParentDice
    {
        public override string ClassName => "DecreaseHealth";
        public readonly Random _random = new();

        public DecreaseHealth(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
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
            var healthDecrease = _random.Next(
                Convert.ToInt32(_config.Dices.DecreaseHealth.MinHealth),
                Convert.ToInt32(_config.Dices.DecreaseHealth.MaxHealth) + 1
            );
            // check if health-decrease should be able to kill the player
            if (_config.Dices.DecreaseHealth.PreventDeath
                && player.Pawn.Value.Health - healthDecrease <= 0)
            {
                healthDecrease = player.Pawn.Value.Health - 1;
            }
            // decrease health
            player.Pawn.Value.Health -= healthDecrease;
            Utilities.SetStateChanged(player.Pawn.Value, "CBaseEntity", "m_iHealth");
            // create GUI for player
            Dictionary<string, string> data = new()
            {
                { "playerName", player.PlayerName },
                { "healthDecrease", healthDecrease.ToString() }
            };
            _players.Add(player, new Dictionary<string, CPointWorldText?>
            {
                { "gui", CreateMainGUI(player, ClassName, data) }
            });
        }
    }
}
