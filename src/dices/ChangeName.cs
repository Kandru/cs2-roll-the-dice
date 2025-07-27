using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class ChangeName : ParentDice
    {
        public override string _className => "ChangeName";
        public readonly Random _random = new();
        private readonly Dictionary<CCSPlayerController, string> _oldNames = [];

        public ChangeName(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", _className));
        }

        public override void Add(CCSPlayerController player)
        {
            // check if player is valid and has a pawn
            if (player == null
                || !player.IsValid
                || player.Pawn?.IsValid == false
                || player.Pawn?.Value?.WeaponServices == null)
            {
                return;
            }
            // get random player name
            List<string> PlayerNames = [.. Utilities.GetPlayers()
                .Where(p => p.IsValid && !p.IsBot && !p.IsHLTV && p != player)
                .Select(p => p.PlayerName)];
            // save old name
            _oldNames[player] = player.PlayerName;
            // set random player name
            ChangePlayerName(player, PlayerNames[_random.Next(PlayerNames.Count)]);
            // create GUI for player
            Dictionary<string, string> data = new()
            {
                { "playerName", _oldNames[player] },
                { "randomName", player.PlayerName }
            };
            _players.Add(player, new Dictionary<string, CPointWorldText?>
            {
                { "gui", CreateMainGUI(player, _className, data) }
            });
        }

        public override void Remove(CCSPlayerController player)
        {
            GUI.RemoveGUIs([.. _players[player].Values.Cast<CPointWorldText>()]);
            ChangePlayerName(player, _oldNames[player]);
            _ = _players.Remove(player);
            _ = _oldNames.Remove(player);
        }

        public override void Destroy()
        {
            Console.WriteLine(_localizer["dice.class.destroy"].Value.Replace("{name}", _className));
            // remove all GUIs for all players
            foreach (KeyValuePair<CCSPlayerController, Dictionary<string, CPointWorldText?>> kvp in _players)
            {
                ChangePlayerName(kvp.Key, _oldNames[kvp.Key]);
                GUI.RemoveGUIs([.. kvp.Value.Values.Cast<CPointWorldText>()]);
            }
            _players.Clear();
            _oldNames.Clear();
        }

        private static void ChangePlayerName(CCSPlayerController player, string newName)
        {
            if (player == null || !player.IsValid || !player.Pawn?.IsValid == true)
            {
                return;
            }
            player.PlayerName = newName;
            Utilities.SetStateChanged(player, "CBasePlayerController", "m_iszPlayerName");
        }
    }
}
