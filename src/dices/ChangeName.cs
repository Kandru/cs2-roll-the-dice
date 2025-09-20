using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices
{
    public class ChangeName : DiceBlueprint
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
            if (PlayerNames.Count > 0)
            {
                ChangePlayerName(player, PlayerNames[_random.Next(PlayerNames.Count)]);
            }
            else if (_config.Dices.ChangeName.PlayerNames.Count > 0)
            {
                string[] randomNames = [.. _config.Dices.ChangeName.PlayerNames];
                if (randomNames.Length == 0)
                {
                    return;
                }

                ChangePlayerName(player, randomNames[_random.Next(randomNames.Length)]);
            }

            _players.Add(player);
            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", _oldNames[player] },
                { "randomName", player.PlayerName }
            });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            ChangePlayerName(player, _oldNames[player]);
            _ = _players.Remove(player);
            _ = _oldNames.Remove(player);
        }

        public override void Reset()
        {
            foreach (CCSPlayerController player in _players.ToList())
            {
                ChangePlayerName(player, _oldNames[player]);
            }
            _players.Clear();
            _oldNames.Clear();
        }

        public override void Destroy()
        {
            Reset();
        }

        private static void ChangePlayerName(CCSPlayerController? player, string newName)
        {
            if (
                player == null
                || !player.IsValid
                || !player.Pawn?.IsValid == true)
            {
                return;
            }
            player.PlayerName = newName;
            Utilities.SetStateChanged(player, "CBasePlayerController", "m_iszPlayerName");
        }
    }
}
