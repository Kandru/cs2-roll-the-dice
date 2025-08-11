using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;

namespace RollTheDice.Dices
{
    public class ChangeName : ParentDice
    {
        public override string ClassName => "ChangeName";
        public readonly Random _random = new();
        private readonly Dictionary<CCSPlayerController, string> _oldNames = [];

        public ChangeName(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
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
            // save old name
            _oldNames[player] = player.PlayerName;
            // set random player name
            ChangePlayerName(player, PlayerNames[_random.Next(PlayerNames.Count)]);
            _players.Add(player);
            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", _oldNames[player] },
                { "randomName", player.PlayerName }
            });
        }

        public override void Remove(CCSPlayerController player)
        {
            ChangePlayerName(player, _oldNames[player]);
            _ = _players.Remove(player);
            _ = _oldNames.Remove(player);
        }

        public override void Destroy()
        {
            Console.WriteLine(_localizer["dice.class.destroy"].Value.Replace("{name}", ClassName));
            foreach (CCSPlayerController player in _players)
            {
                ChangePlayerName(player, _oldNames[player]);
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
