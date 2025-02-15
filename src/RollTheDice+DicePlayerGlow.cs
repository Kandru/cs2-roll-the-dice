using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace RollTheDice
{
    public partial class RollTheDice : BasePlugin
    {
        private Dictionary<CCSPlayerController, (uint, uint)> _playersThatAreGlowing = new();

        private Dictionary<string, string> DicePlayerGlow(CCSPlayerController player, CCSPlayerPawn playerPawn)
        {
            CDynamicProp? modelRelay = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
            CDynamicProp? modelGlow = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
            if (modelGlow != null && modelRelay != null)
            {
                string modelName = playerPawn.CBodyComponent!.SceneNode!.GetSkeletonInstance().ModelState.ModelName;

                modelRelay.Spawnflags = 256u;
                modelRelay.RenderMode = RenderMode_t.kRenderNone;
                modelRelay.SetModel(modelName);
                modelRelay.AcceptInput("FollowEntity", playerPawn, modelRelay, "!activator");
                modelRelay.DispatchSpawn();

                modelGlow.SetModel(modelName);
                modelGlow.AcceptInput("FollowEntity", modelRelay, modelGlow, "!activator");
                modelGlow.DispatchSpawn();
                modelGlow.Render = Color.FromArgb(1, 255, 255, 255);
                if (playerPawn.TeamNum == (int)CsTeam.Terrorist) modelGlow.Glow.GlowColorOverride = Color.Red;
                else
                    modelGlow.Glow.GlowColorOverride = Color.Blue;
                modelGlow.Spawnflags = 256u;
                modelGlow.RenderMode = RenderMode_t.kRenderGlow;
                modelGlow.Glow.GlowRange = 5000;
                modelGlow.Glow.GlowTeam = -1;
                modelGlow.Glow.GlowType = 3;
                modelGlow.Glow.GlowRangeMin = 30;

                _playersThatAreGlowing.Add(player, (modelRelay.Index, modelGlow.Index));
            }
            return new Dictionary<string, string>
            {
                { "playerName", player.PlayerName }
            };
        }

        private void DicePlayerGlowReset()
        {
            Dictionary<CCSPlayerController, (uint, uint)> _playersThatAreGlowingCopy = new(_playersThatAreGlowing);
            foreach (var player in _playersThatAreGlowingCopy.Keys)
            {
                try
                {
                    if (player == null
                        || player.PlayerPawn == null
                        || !player.PlayerPawn.IsValid
                        || player.PlayerPawn.Value == null
                        || player.LifeState != (byte)LifeState_t.LIFE_ALIVE) continue;
                    var (modelRelayIndex, modelGlowIndex) = _playersThatAreGlowingCopy[player];
                    RemoveProp((int)modelRelayIndex);
                    RemoveProp((int)modelGlowIndex);
                }
                catch
                {
                    // do nothing
                }
            }
            _playersThatAreGlowing.Clear();
        }

        private void DicePlayerGlowResetForPlayer(CCSPlayerController player)
        {
            if (!_playersThatAreGlowing.ContainsKey(player)) return;
            var (modelRelayIndex, modelGlowIndex) = _playersThatAreGlowing[player];
            RemoveProp((int)modelRelayIndex);
            RemoveProp((int)modelGlowIndex);
            _playersThatAreGlowing.Remove(player);
        }
    }
}
