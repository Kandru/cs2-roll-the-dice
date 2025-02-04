using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace RollTheDice
{
    public partial class RollTheDice : BasePlugin
    {
        private readonly Dictionary<CCSPlayerController, int> _playersWithHostageSounds = new();
        private readonly List<string> _hostageSounds = new List<string>
        {
            "Hostage.Pain"
        };

        private Dictionary<string, string> DicePlayerMakeHostageSounds(CCSPlayerController player, CCSPlayerPawn playerPawn)
        {
            // create listener if not exists
            if (_playersWithHostageSounds.Count() == 0) RegisterListener<Listeners.OnTick>(EventDicePlayerMakeHostageSoundsOnTick);
            // add player to list
            _playersWithHostageSounds.Add(player, 0);
            return new Dictionary<string, string>
            {
                { "playerName", player.PlayerName }
            };
        }

        private void DicePlayerMakeHostageSoundsUnload()
        {
            DicePlayerMakeHostageSoundsReset();
        }

        private void DicePlayerMakeHostageSoundsReset()
        {
            RemoveListener<Listeners.OnTick>(EventDicePlayerMakeHostageSoundsOnTick);
            _playersWithHostageSounds.Clear();
        }

        private void DicePlayerMakeHostageSoundsResetForPlayer(CCSPlayerController player)
        {
            if (!_playersWithHostageSounds.ContainsKey(player)) return;
            _playersWithHostageSounds.Remove(player);
        }

        private void EventDicePlayerMakeHostageSoundsOnTick()
        {
            if (_playersWithHostageSounds.Count() == 0) return;
            // worker
            Dictionary<CCSPlayerController, int> _playersWithHostageSoundsCopy = new(_playersWithHostageSounds);
            foreach (var (player, playerStatus) in _playersWithHostageSoundsCopy)
            {
                try
                {
                    // sanity checks
                    if (player == null
                    || player.PlayerPawn == null || !player.PlayerPawn.IsValid || player.PlayerPawn.Value == null
                    || player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE) continue;
                    if (player.Buttons == 0 && playerStatus == 0)
                    {
                        // emit sound
                        EmitSound(player, _hostageSounds[_random.Next(_hostageSounds.Count)]);
                        _playersWithHostageSounds[player] = (int)Server.CurrentTime + 1;
                    }
                    else if (player.Buttons != 0 && playerStatus <= (int)Server.CurrentTime)
                    {
                        _playersWithHostageSounds[player] = 0;
                    }
                }
                catch (Exception e)
                {
                    // remove player
                    _playersWithHostageSounds.Remove(player);
                    // log error
                    Console.WriteLine(Localizer["core.error"].Value.Replace("{error}", e.Message));
                }
            }
        }
    }
}
