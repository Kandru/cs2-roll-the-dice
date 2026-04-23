using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices
{
    public class Jammer : DiceBlueprint
    {
        public override string ClassName => "Jammer";
        public readonly Random _random = new();

        public Jammer(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
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
            // hide hud
            SetHUDVisibility(player, false);
            _players.Add(player);
            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName }
            });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            SetHUDVisibility(player, true);
            _ = _players.Remove(player);
        }

        public override void Reset()
        {
            foreach (CCSPlayerController player in _players.ToList())
            {
                Remove(player);
            }
            _players.Clear();
        }

        public override void Destroy()
        {
            Reset();
        }

        private static void SetHUDVisibility(CCSPlayerController player, bool enabled)
        {
            if (player.Pawn?.Value == null
                || !player.Pawn.Value.IsValid)
            {
                return;
            }
            player.Pawn.Value.HideHUD = (uint)(enabled
                ? (player.Pawn.Value.HideHUD & ~(1 << 8))
                : (player.Pawn.Value.HideHUD | (1 << 8)));
            Utilities.SetStateChanged(player.Pawn.Value, "CBasePlayerPawn", "m_iHideHUD");
        }
    }
}
