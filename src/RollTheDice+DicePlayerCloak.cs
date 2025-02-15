using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using System.Drawing;

namespace RollTheDice
{
    public partial class RollTheDice : BasePlugin
    {
        private Dictionary<CCSPlayerController, int> _playersWithCloak = new();

        private Dictionary<string, string> DicePlayerCloak(CCSPlayerController player, CCSPlayerPawn playerPawn)
        {
            // create listener if not exists
            if (_playersWithCloak.Count() == 0) RegisterListener<Listeners.OnTick>(EventDicePlayerCloakOnTick);
            // add player to list
            _playersWithCloak.Add(player, 255);
            return new Dictionary<string, string>
            {
                { "playerName", player.PlayerName }
            };
        }

        private void DicePlayerCloakUnload()
        {
            DicePlayerCloakReset();
        }

        private void DicePlayerCloakReset()
        {
            RemoveListener<Listeners.OnTick>(EventDicePlayerCloakOnTick);
            // iterate through all players
            Dictionary<CCSPlayerController, int> _playersWithCloakCopy = new(_playersWithCloak);
            foreach (var (player, visibility) in _playersWithCloakCopy)
            {
                try
                {
                    if (player == null
                        || player.PlayerPawn == null
                        || !player.PlayerPawn.IsValid
                        || player.PlayerPawn.Value == null
                        || player.LifeState != (byte)LifeState_t.LIFE_ALIVE) continue;
                    // get player pawn
                    var playerPawn = player.PlayerPawn.Value!;
                    // reset player render color
                    playerPawn.Render = Color.FromArgb(255, 255, 255, 255);
                    // set state changed
                    Utilities.SetStateChanged(playerPawn, "CBaseModelEntity", "m_clrRender");
                }
                catch
                {
                    // do nothing
                }
            }
            _playersWithCloak.Clear();
        }

        private void DicePlayerCloakResetForPlayer(CCSPlayerController player)
        {
            if (player.PlayerPawn == null
                || !player.PlayerPawn.IsValid
                || player.PlayerPawn.Value == null) return;
            if (!_playersWithCloak.ContainsKey(player)) return;
            // reset player render color
            player.PlayerPawn.Value.Render = Color.FromArgb(255, 255, 255, 255);
            // set state changed
            Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseModelEntity", "m_clrRender");
            _playersWithCloak.Remove(player);
        }

        private void EventDicePlayerCloakOnTick()
        {
            if (_playersWithCloak.Count() == 0) return;
            // worker
            Dictionary<CCSPlayerController, int> _playersWithCloakCopy = new(_playersWithCloak);
            foreach (var (player, visibility) in _playersWithCloakCopy)
            {
                bool changedVisibility = false;
                try
                {
                    // sanity checks
                    if (player == null
                    || player.Pawn == null
                    || player.Pawn.Value == null
                    || player.PlayerPawn == null || !player.PlayerPawn.IsValid || player.PlayerPawn.Value == null
                    || player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE) continue;
                    // check if player does not move
                    if (player.Buttons == 0 && visibility > 0)
                    {
                        // update visibility variable
                        _playersWithCloak[player]--;
                        // reset player render color
                        player.PlayerPawn.Value.Render = Color.FromArgb(visibility - 1, 255, 255, 255);
                        // set state changed
                        Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseModelEntity", "m_clrRender");
                        changedVisibility = true;
                    }
                    else if (player.Buttons != 0 && visibility < 255)
                    {
                        // update visibility variable
                        _playersWithCloak[player] = 255;
                        // reset player render color
                        player.PlayerPawn.Value.Render = Color.FromArgb(255, 255, 255, 255);
                        // set state changed
                        Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseModelEntity", "m_clrRender");
                        changedVisibility = true;
                    }
                    // update gui if available
                    if (changedVisibility)
                        if (_playersThatRolledTheDice.ContainsKey(player)
                            && _playersThatRolledTheDice[player].ContainsKey("gui_status")
                            && (CPointWorldText)_playersThatRolledTheDice[player]["gui_status"] != null)
                        {
                            string percentageVisible = ((_playersWithCloak[player] / 255.0) * 100).ToString("0.#") + "%";
                            CPointWorldText worldText = (CPointWorldText)_playersThatRolledTheDice[player]["gui_status"];
                            worldText.AcceptInput(
                                "SetMessage",
                                worldText,
                                worldText,
                                Localizer["DicePlayerCloak_gui_status"].Value.Replace("{percentage}", percentageVisible)
                            );
                        }
                }
                catch (Exception e)
                {
                    // remove player
                    _playersWithCloak.Remove(player);
                    // log error
                    Console.WriteLine(Localizer["core.error"].Value.Replace("{error}", e.Message));
                }
            }
        }
    }
}
