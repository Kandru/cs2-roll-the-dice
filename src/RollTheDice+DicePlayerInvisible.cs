using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using System.Drawing;

namespace RollTheDice
{
    public partial class RollTheDice : BasePlugin
    {
        private List<CCSPlayerController> _playersThatAreInvisible = new();

        private Dictionary<string, string> DicePlayerInvisible(CCSPlayerController player, CCSPlayerPawn playerPawn)
        {
            Dictionary<string, object> config = GetDiceConfig("DicePlayerInvisible");
            _playersThatAreInvisible.Add(player);
            int alpha = 255 - (int)((float)config["invisibility_percentage"] * 255);
            alpha = Math.Clamp(alpha, 0, 255);
            playerPawn.Render = Color.FromArgb(alpha, 255, 255, 255);
            Utilities.SetStateChanged(playerPawn, "CBaseModelEntity", "m_clrRender");
            return new Dictionary<string, string>
            {
                { "playerName", player.PlayerName }
            };
        }

        private void DicePlayerInvisibleUnload()
        {
            DicePlayerInvisibleReset();
        }

        private void DicePlayerInvisibleReset()
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

        private Dictionary<string, object> DicePlayerInvisibleConfig()
        {
            var config = new Dictionary<string, object>();
            config["invisibility_percentage"] = (float)0.5f;
            return config;
        }
    }
}
