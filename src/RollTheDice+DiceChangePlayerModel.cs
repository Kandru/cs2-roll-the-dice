using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace RollTheDice
{
    public partial class RollTheDice : BasePlugin
    {
        private Dictionary<CCSPlayerController, string> _playersWithChangedPlayerModel = new();

        private Dictionary<string, string> DiceChangePlayerModel(CCSPlayerController player, CCSPlayerPawn playerPawn)
        {
            string playerModel = playerPawn.CBodyComponent!.SceneNode!.GetSkeletonInstance().ModelState.ModelName;
            _playersWithChangedPlayerModel.Add(player, playerModel);
            // set new player model
            if (playerPawn.TeamNum == (int)CsTeam.Terrorist)
            {
                playerPawn.SetModel("characters/models/ctm_sas/ctm_sas.vmdl");
            }
            else
            {
                playerPawn.SetModel("characters/models/tm_phoenix/tm_phoenix.vmdl");
            }
            return new Dictionary<string, string>
            {
                { "playerName", player.PlayerName }
            };
        }

        private void DiceChangePlayerModelUnload()
        {
            DiceChangePlayerModelReset();
        }

        private void DiceChangePlayerModelReset()
        {
            // iterate through all players
            Dictionary<CCSPlayerController, string> _playersWithChangedPlayerModelCopy = new(_playersWithChangedPlayerModel);
            foreach (var kvp in _playersWithChangedPlayerModelCopy)
            {
                try
                {
                    var player = kvp.Key;
                    var model = kvp.Value;
                    if (player == null
                        || player.PlayerPawn == null
                        || !player.PlayerPawn.IsValid
                        || player.PlayerPawn.Value == null
                        || player.LifeState != (byte)LifeState_t.LIFE_ALIVE) continue;
                    // get player pawn
                    var playerPawn = player.PlayerPawn.Value!;
                    // reset player model
                    playerPawn.SetModel(model);
                }
                catch
                {
                    // do nothing
                }
            }
            _playersWithChangedPlayerModel.Clear();
        }

        private void DiceChangePlayerModelResetForPlayer(CCSPlayerController player)
        {
            if (!_playersWithChangedPlayerModel.ContainsKey(player)) return;
            if (player == null
                || player.PlayerPawn == null
                || !player.PlayerPawn.IsValid
                || player.PlayerPawn.Value == null
                || player.LifeState != (byte)LifeState_t.LIFE_ALIVE) return;
            // get player pawn
            var playerPawn = player.PlayerPawn.Value!;
            // reset player model
            playerPawn.SetModel(_playersWithChangedPlayerModel[player]);
            _playersWithChangedPlayerModel.Remove(player);
        }
    }
}
