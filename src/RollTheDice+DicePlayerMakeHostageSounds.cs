using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace RollTheDice
{
    public partial class RollTheDice : BasePlugin
    {
        private readonly Dictionary<CCSPlayerController, Dictionary<string, int>> _playersWithHostageSounds = new();
        private readonly List<string> _hostageSounds = new List<string>
        {
            "Hostage.Pain"
        };

        private Dictionary<string, string> DicePlayerMakeHostageSounds(CCSPlayerController player, CCSPlayerPawn playerPawn)
        {
            // create listener if not exists
            if (_playersWithHostageSounds.Count() == 0) RegisterListener<Listeners.OnTick>(EventDicePlayerMakeHostageSoundsOnTick);
            // add player to list
            _playersWithHostageSounds.Add(player, new Dictionary<string, int>
            {
                { "cooldown", 0 },
                { "delay", 0 }
            });
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
            // check if player has this dice
            if (!_playersWithHostageSounds.ContainsKey(player)) return;
            // remove player
            _playersWithHostageSounds.Remove(player);
            // remove listener if no players have this dice
            if (_playersWithHostageSounds.Count == 0) DicePlayerMakeHostageSoundsReset();
        }

        private void EventDicePlayerMakeHostageSoundsOnTick()
        {
            if (_playersWithHostageSounds.Count() == 0) return;
            // worker
            Dictionary<CCSPlayerController, Dictionary<string, int>> _playersWithHostageSoundsCopy = new(_playersWithHostageSounds);
            foreach (var (player, playerData) in _playersWithHostageSoundsCopy)
            {
                try
                {
                    // sanity checks
                    if (player == null
                    || player.PlayerPawn == null || !player.PlayerPawn.IsValid || player.PlayerPawn.Value == null
                    || player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE) continue;
                    if (player.Buttons == 0
                        && playerData["cooldown"] > -1
                        && playerData["cooldown"] <= (int)Server.CurrentTime
                        || (playerData["delay"] <= (int)Server.CurrentTime))
                    {
                        // emit sound
                        EmitSound(player, _hostageSounds[_random.Next(_hostageSounds.Count)]);
                        // set random next sound delay
                        Dictionary<string, object> config = GetDiceConfig("DicePlayerMakeHostageSounds");
                        _playersWithHostageSounds[player]["cooldown"] = -1;
                        _playersWithHostageSounds[player]["delay"] = (int)Server.CurrentTime + _random.Next(
                            Convert.ToInt32(config["min_sound_delay"]),
                            Convert.ToInt32(config["max_sound_delay"])
                        );
                    }
                    else if (player.Buttons != 0 && playerData["cooldown"] == -1)
                    {
                        _playersWithHostageSounds[player]["cooldown"] = (int)Server.CurrentTime;

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

        private Dictionary<string, object> DicePlayerMakeHostageSoundsConfig()
        {
            var config = new Dictionary<string, object>();
            config["min_sound_delay"] = (int)5;
            config["max_sound_delay"] = (int)10;
            return config;
        }
    }
}
