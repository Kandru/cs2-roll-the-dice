using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using System.Drawing;

namespace RollTheDice.Dices
{
    public class Invisible : DiceBlueprint
    {
        public override string ClassName => "Invisible";

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
            player.Pawn.Value.Render = Color.FromArgb(alpha, 255, 255, 255);
            Utilities.SetStateChanged(player.Pawn.Value, "CBaseModelEntity", "m_clrRender");
            _players.Add(player);
            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName }
            });
        }

        public override void Remove(CCSPlayerController player)
        {
            _ = _players.Remove(player);
            if (player?.Pawn?.Value == null
                || !player.Pawn.IsValid
                || player.Pawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE) return;
            // reset player render color
            player.Pawn.Value.Render = Color.FromArgb(255, 255, 255, 255);
            // set state changed
            Utilities.SetStateChanged(player.Pawn.Value, "CBaseModelEntity", "m_clrRender");
        }

        public override void Destroy()
        {
            Console.WriteLine(_localizer["dice.class.destroy"].Value.Replace("{name}", ClassName));
            foreach (CCSPlayerController player in _players)
            {
                if (player?.Pawn?.Value == null
                    || !player.Pawn.IsValid
                    || player.Pawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE) continue;
                // reset player render color
                player.Pawn.Value.Render = Color.FromArgb(255, 255, 255, 255);
                // set state changed
                Utilities.SetStateChanged(player.Pawn.Value, "CBaseModelEntity", "m_clrRender");
            }
            _players.Clear();
        }
    }
}
