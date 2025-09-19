using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using System.Drawing;

namespace RollTheDice.Dices
{
    public class Invisible : DiceBlueprint
    {
        public override string ClassName => "Invisible";
        public override List<string> Events => [
            "EventPlayerDeath",
        ];

        public Invisible(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
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
            // set invisibility
            int alpha = 255 - (int)(_config.Dices.Invisible.PercentageVisible * 255);
            alpha = Math.Clamp(alpha, 0, 255);
            SetPlayerInvisible(player, alpha);
            _players.Add(player);
            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName }
            });
        }

        public override void Remove(CCSPlayerController player)
        {
            _ = _players.Remove(player);
            SetPlayerInvisible(player, 255);
        }

        public override void Reset()
        {
            foreach (CCSPlayerController player in _players)
            {
                Remove(player);
            }
            _players.Clear();
        }

        public override void Destroy()
        {
            Reset();
        }

        public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (player == null
                || !_players.Contains(player)
                || player.PlayerPawn?.Value?.WeaponServices == null)
            {
                return HookResult.Continue;
            }
            Remove(player);
            return HookResult.Continue;
        }

        private void SetPlayerInvisible(CCSPlayerController? player, int alpha)
        {
            if (player?.Pawn?.Value == null
                || !player.Pawn.Value.IsValid)
            {
                return;
            }
            player.Pawn.Value.Render = Color.FromArgb(alpha, 255, 255, 255);
            Utilities.SetStateChanged(player.Pawn.Value, "CBaseModelEntity", "m_clrRender");
        }
    }
}
