using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using System.Drawing;

namespace RollTheDice
{
    public partial class RollTheDice : BasePlugin
    {
        private List<CCSPlayerController> _playersThatAreInvisible = new();

        private string DicePlayerInvisible(CCSPlayerController player, CCSPlayerPawn playerPawn)
        {
            _playersThatAreInvisible.Add(player);
            playerPawn.Render = Color.FromArgb(125, 255, 255, 255);
            Utilities.SetStateChanged(playerPawn, "CBaseModelEntity", "m_clrRender");
            return Localizer["DicePlayerInvisible"].Value
                .Replace("{playerName}", player.PlayerName);
        }

        private void ResetDicePlayerInvisible()
        {
            // iterate through all players
            foreach (var player in _playersThatAreInvisible)
            {
                if (player == null || player.PlayerPawn == null || !player.PlayerPawn.IsValid || player.PlayerPawn.Value == null || player.LifeState != (byte)LifeState_t.LIFE_ALIVE) continue;
                // get player pawn
                var playerPawn = player.PlayerPawn.Value!;
                // reset player render color
                playerPawn.Render = Color.FromArgb(255, 255, 255, 255);
                // set state changed
                Utilities.SetStateChanged(playerPawn, "CBaseModelEntity", "m_clrRender");
            }
            _playersThatAreInvisible.Clear();
        }
    }
}
