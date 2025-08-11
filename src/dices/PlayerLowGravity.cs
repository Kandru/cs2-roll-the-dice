using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;

namespace RollTheDice.Dices
{
    public class PlayerLowGravity : ParentDice
    {
        public override string ClassName => "PlayerLowGravity";
        public readonly Random _random = new();

        public PlayerLowGravity(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
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
            // set low gravity
            player.Pawn.Value.GravityScale = _config.Dices.PlayerLowGravity.GravityScale;
            // create GUI for player
            Dictionary<string, string> data = new()
            {
                { "playerName", player.PlayerName }
            };
            _players.Add(player, new Dictionary<string, CPointWorldText?>
            {
                //{ "gui", CreateMainGUI(player, ClassName, data) }
            });
        }
    }
}
