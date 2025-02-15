using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace RollTheDice
{
    public partial class RollTheDice : BasePlugin
    {
        private List<CCSPlayerPawn> _playersWithChangedModelSize = new();

        private Dictionary<string, string> DiceChangePlayerSize(CCSPlayerController player, CCSPlayerPawn playerPawn)
        {
            Dictionary<string, object> config = GetDiceConfig("DiceChangePlayerSize");
            _playersWithChangedModelSize.Add(playerPawn);
            float playerSize = float.Round((float)(_random.NextDouble() * ((float)config["max_size"] - (float)config["min_size"]) + (float)config["min_size"]), 2);
            var playerSceneNode = playerPawn.CBodyComponent?.SceneNode;
            if (playerSceneNode == null)
                return new Dictionary<string, string>
                {
                    {"error", "command.rollthedice.error"}
                };
            playerSceneNode.GetSkeletonInstance().Scale = playerSize;
            playerPawn.AcceptInput("SetScale", null, null, playerSize.ToString());
            Server.NextFrame(() =>
            {
                if (playerPawn == null) return;
                Utilities.SetStateChanged(playerPawn, "CBaseEntity", "m_CBodyComponent");
            });

            return new Dictionary<string, string>
            {
                { "playerName", player.PlayerName },
                { "playerSize", playerSize.ToString() }
            };
        }

        private void DiceChangePlayerSizeUnload()
        {
            DiceChangePlayerSizeReset();
        }

        private void DiceChangePlayerSizeReset()
        {
            List<CCSPlayerPawn> _playersWithChangedModelSizeCopy = new(_playersWithChangedModelSize);
            foreach (CCSPlayerPawn playerPawn in _playersWithChangedModelSizeCopy)
            {
                try
                {
                    if (playerPawn == null) continue;
                    var playerSceneNode = playerPawn.CBodyComponent?.SceneNode;
                    if (playerSceneNode == null) continue;
                    playerSceneNode.GetSkeletonInstance().Scale = 1.0f;
                    playerPawn.AcceptInput("SetScale", null, null, "1.0");
                    Server.NextFrame(() =>
                    {
                        if (playerPawn == null) return;
                        Utilities.SetStateChanged(playerPawn, "CBaseEntity", "m_CBodyComponent");
                    });
                }
                catch
                {
                    // do nothing
                }
            }
            _playersWithChangedModelSize.Clear();
        }

        private void DiceChangePlayerSizeResetForPlayer(CCSPlayerController player)
        {
            if (player.PlayerPawn == null
                || !player.PlayerPawn.IsValid
                || player.PlayerPawn.Value == null) return;
            if (!_playersWithChangedModelSize.Contains(player.PlayerPawn.Value)) return;
            var playerSceneNode = player.PlayerPawn.Value.CBodyComponent?.SceneNode;
            if (playerSceneNode == null) return;
            playerSceneNode.GetSkeletonInstance().Scale = 1.0f;
            player.PlayerPawn.Value.AcceptInput("SetScale", null, null, "1.0");
            Server.NextFrame(() =>
            {
                if (player.PlayerPawn.Value == null) return;
                Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseEntity", "m_CBodyComponent");
            });
            _playersWithChangedModelSize.Remove(player.PlayerPawn.Value);
        }

        private Dictionary<string, object> DiceChangePlayerSizeConfig()
        {
            var config = new Dictionary<string, object>();
            config["min_size"] = (float)0.5f;
            config["max_size"] = (float)1.5f;
            return config;
        }
    }
}
