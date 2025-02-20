using CounterStrikeSharp.API.Core;

namespace RollTheDice
{
    public partial class RollTheDice : BasePlugin
    {
        private Dictionary<string, string> DicePlayerHighGravity(CCSPlayerController player, CCSPlayerPawn playerPawn)
        {
            Dictionary<string, object> config = GetDiceConfig("DicePlayerHighGravity");
            playerPawn.GravityScale = (float)config["gravity_scale"];
            return new Dictionary<string, string>
            {
                { "playerName", player.PlayerName }
            };
        }

        private void DicePlayerHighGravityResetForPlayer(CCSPlayerController player)
        {
            if (player == null
                || player.PlayerPawn == null
                || !player.PlayerPawn.IsValid
                || player.PlayerPawn.Value == null) return;
            player.PlayerPawn.Value.GravityScale = 1.0f;
        }

        private Dictionary<string, object> DicePlayerHighGravityConfig()
        {
            var config = new Dictionary<string, object>();
            config["gravity_scale"] = (float)4.0f;
            return config;
        }
    }
}