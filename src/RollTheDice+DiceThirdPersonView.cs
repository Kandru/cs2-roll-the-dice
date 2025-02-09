using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;

// thx to https://github.com/grrhn/ThirdPerson-WIP/ (license GPLv3)
// initial dice idea by https://github.com/TangoCash/cs2-roll-the-dice/blob/main/src/RollTheDice%2BDiceThirdPersonView.cs
// in issue https://github.com/Kandru/cs2-roll-the-dice/issues/4

namespace RollTheDice
{
    public partial class RollTheDice : BasePlugin
    {
        private static Dictionary<CCSPlayerController, CDynamicProp> _playersWithThirdPersonView = new();

        private Dictionary<string, string> DiceThirdPersonView(CCSPlayerController player, CCSPlayerPawn playerPawn)
        {
            CDynamicProp? _cameraProp = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
            if (_cameraProp == null) return new Dictionary<string, string>
            {
                {"error", "command.rollthedice.error"}
            };
            if (_playersWithThirdPersonView.Count() == 0)
            {
                RegisterListener<Listeners.OnTick>(DiceThirdPersonViewOnTick);
            }
            _cameraProp.DispatchSpawn();
            _cameraProp.Render = Color.FromArgb(0, 255, 255, 255);
            _cameraProp.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_NEVER;
            _cameraProp.Collision.SolidFlags = 12;
            _cameraProp.Collision.SolidType = SolidType_t.SOLID_VPHYSICS;
            Utilities.SetStateChanged(_cameraProp, "CBaseModelEntity", "m_clrRender");
            _cameraProp.Teleport(CalculatePositionInFront(player, -110, 90), playerPawn.V_angle);
            playerPawn.CameraServices!.ViewEntity.Raw = _cameraProp.EntityHandle.Raw;
            Utilities.SetStateChanged(playerPawn!, "CBasePlayerPawn", "m_pCameraServices");
            // add player to list
            _playersWithThirdPersonView.Add(player, _cameraProp);
            return new Dictionary<string, string>
            {
                { "playerName", player.PlayerName }
            };
        }

        private void DiceThirdPersonViewUnload()
        {
            DiceThirdPersonViewReset();
        }

        private void DiceThirdPersonViewReset()
        {
            RemoveListener<Listeners.OnTick>(DiceThirdPersonViewOnTick);
            // iterate through all players
            Dictionary<CCSPlayerController, CDynamicProp> _playersWithThirdPersonViewCopy = new(_playersWithThirdPersonView);
            foreach (CCSPlayerController player in _playersWithThirdPersonViewCopy.Keys)
            {
                if (player == null
                    || player.PlayerPawn == null
                    || !player.PlayerPawn.IsValid
                    || player.PlayerPawn.Value == null
                    || _playersWithThirdPersonViewCopy[player] == null) continue;
                player!.PlayerPawn!.Value!.CameraServices!.ViewEntity.Raw = uint.MaxValue;
                Utilities.SetStateChanged(player.PlayerPawn.Value, "CBasePlayerPawn", "m_pCameraServices");
                _playersWithThirdPersonViewCopy[player].AcceptInput("Kill");
            }
            _playersWithThirdPersonView.Clear();
        }

        private void DiceThirdPersonViewResetForPlayer(CCSPlayerController player)
        {
            if (!_playersWithThirdPersonView.ContainsKey(player)) return;
            player!.PlayerPawn!.Value!.CameraServices!.ViewEntity.Raw = uint.MaxValue;
            Utilities.SetStateChanged(player.PlayerPawn.Value, "CBasePlayerPawn", "m_pCameraServices");
            _playersWithThirdPersonView[player].AcceptInput("Kill");
            _playersWithThirdPersonView.Remove(player);
        }

        private void DiceThirdPersonViewOnTick()
        {
            // remove listener if no players to save resources
            if (_playersWithThirdPersonView.Count() == 0) return;
            // worker
            Dictionary<CCSPlayerController, CDynamicProp> _playersWithThirdPersonViewCopy = new(_playersWithThirdPersonView);
            foreach (CCSPlayerController player in _playersWithThirdPersonViewCopy.Keys)
            {
                try
                {
                    // sanity checks
                    if (player == null
                    || player.Pawn == null
                    || player.Pawn.Value == null
                    || player.PlayerPawn == null || !player.PlayerPawn.IsValid || player.PlayerPawn.Value == null
                    || player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE
                    || _playersWithThirdPersonViewCopy[player] == null) continue;
                    UpdateCamera(_playersWithThirdPersonViewCopy[player], player);
                }
                catch (Exception e)
                {
                    // remove player
                    _playersWithThirdPersonView.Remove(player);
                    // log error
                    Console.WriteLine(Localizer["core.error"].Value.Replace("{error}", e.Message));
                }
            }
        }
    }
}