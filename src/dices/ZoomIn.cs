using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices
{
    public class ZoomIn : DiceBlueprint
    {
        public override string ClassName => "ZoomIn";
        public readonly Random _random = new();
        private readonly Dictionary<CCSPlayerController, uint> _oldFOVs = [];

        public ZoomIn(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
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
            uint min = _config.Dices.ZoomIn.MinZoomFactor;
            uint max = _config.Dices.ZoomIn.MaxZoomFactor;
            uint zoomFactor = (uint)_random.Next((int)min, (int)max + 1);
            _oldFOVs.Add(player, player.DesiredFOV);
            SetPlayerFOV(player, zoomFactor);
            _players.Add(player);
            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName }
            });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            SetPlayerFOV(player, _oldFOVs[player]);
            _ = _players.Remove(player);
            _ = _oldFOVs.Remove(player);
        }

        public override void Reset()
        {
            foreach (CCSPlayerController player in _players.ToList())
            {
                Remove(player);
            }
            _players.Clear();
            _oldFOVs.Clear();
        }

        private static void SetPlayerFOV(CCSPlayerController player, uint fov)
        {
            if (player == null)
            {
                return;
            }
            player.DesiredFOV = fov;
            Utilities.SetStateChanged(player, "CBasePlayerController", "m_iDesiredFOV");
        }
    }
}
