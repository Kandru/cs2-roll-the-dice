using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace RollTheDice
{
    public partial class RollTheDice : BasePlugin
    {
        private Dictionary<string, string> DicePlayerOneHP(CCSPlayerController player, CCSPlayerPawn playerPawn)
        {
            // set maximum health to players last known health value
            playerPawn.MaxHealth = playerPawn.Health;
            // set health to 1
            playerPawn.Health = 1;
            Utilities.SetStateChanged(playerPawn, "CBaseEntity", "m_iHealth");
            return new Dictionary<string, string>
            {
                { "playerName", player.PlayerName }
            };
        }

        private void DicePlayerOneHPResetForPlayer(CCSPlayerController player)
        {
            if (player == null
                || player.PlayerPawn == null
                || !player.PlayerPawn.IsValid
                || player.PlayerPawn.Value == null) return;
            // reset value to the one before the dice effect
            player.PlayerPawn.Value.Health = player.PlayerPawn.Value.MaxHealth;
        }
    }
}
