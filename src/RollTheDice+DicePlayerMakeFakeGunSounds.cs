using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace RollTheDice
{
    public partial class RollTheDice : BasePlugin
    {
        private readonly Dictionary<CCSPlayerController, int> _playersWithFakeGunSounds = new();
        private readonly List<(string, string, int, float)> _fakeGunSounds = new()
        {
            ("Deagle", "Weapon_DEagle.Single", 3, 2.0f),
            ("M249", "Weapon_M249.Single", 16, 0.7f),
            ("AWP", "Weapon_AWP.Single", 1, 1f),
            ("Bizon", "Weapon_bizon.Single", 10, 1.5f),
            ("P90", "Weapon_P90.Single", 15, 1.1f),
            ("G3SG1", "Weapon_G3SG1.Single", 11, 1.1f),
            ("Negev", "Weapon_Negev.Single", 14, 0.7f),
            ("Nova", "Weapon_Nova.Single", 3, 2.5f),
            ("AUG", "Weapon_AUG.Single", 12, 1.1f),
            ("M4A1", "Weapon_M4A1.Single", 8, 0.9f)
        };

        private Dictionary<string, string> DicePlayerMakeFakeGunSounds(CCSPlayerController player, CCSPlayerPawn playerPawn)
        {
            // create listener if not exists
            if (_playersWithFakeGunSounds.Count == 0) RegisterListener<Listeners.OnTick>(EventDicePlayerMakeFakeGunSoundsOnTick);
            // add player to list
            _playersWithFakeGunSounds.Add(player, (int)Server.CurrentTime + _random.Next(3, 10));
            return new Dictionary<string, string>
            {
                { "playerName", player.PlayerName }
            };
        }

        private void DicePlayerMakeFakeGunSoundsUnload()
        {
            DicePlayerMakeFakeGunSoundsReset();
        }

        private void DicePlayerMakeFakeGunSoundsReset()
        {
            RemoveListener<Listeners.OnTick>(EventDicePlayerMakeFakeGunSoundsOnTick);
            _playersWithFakeGunSounds.Clear();
        }

        private void DicePlayerMakeFakeGunSoundsResetForPlayer(CCSPlayerController player)
        {
            // check if player has this dice
            if (!_playersWithFakeGunSounds.ContainsKey(player)) return;
            // remove player
            _playersWithFakeGunSounds.Remove(player);
            // remove listener if no players have this dice
            if (_playersWithFakeGunSounds.Count == 0) DicePlayerMakeFakeGunSoundsReset();
        }

        private void EventDicePlayerMakeFakeGunSoundsOnTick()
        {
            if (_playersWithFakeGunSounds.Count == 0) return;
            // worker
            Dictionary<CCSPlayerController, int> _playersWithFakeGunSoundsCopy = new(_playersWithFakeGunSounds);
            foreach (var (player, last_sound) in _playersWithFakeGunSoundsCopy)
            {
                try
                {
                    // sanity checks
                    if (last_sound == 0
                        || last_sound >= (int)Server.CurrentTime
                        || player == null
                        || player.PlayerPawn == null || !player.PlayerPawn.IsValid || player.PlayerPawn.Value == null
                        || player.Buttons != 0
                        || player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE) continue;
                    // get random gun sound entry
                    var (weaponName, soundName, playTotal, soundLength) = _fakeGunSounds[_random.Next(_fakeGunSounds.Count)];
                    EmitFakeGunSounds(player.Handle, soundName, soundLength, playTotal);
                    // let the player know
                    string message = Localizer["DicePlayerMakeFakeGunSoundsWeapon_player"].Value.Replace("{weapon}", weaponName);
                    // update gui if available
                    if (_playersThatRolledTheDice.ContainsKey(player)
                        && _playersThatRolledTheDice[player].ContainsKey("gui_status")
                        && (CPointWorldText)_playersThatRolledTheDice[player]["gui_status"] != null)
                    {
                        CPointWorldText worldText = (CPointWorldText)_playersThatRolledTheDice[player]["gui_status"];
                        ChangeColor(worldText, Config.GUIPositions[Config.GUIPosition].StatusColorEnabled);
                        worldText.AcceptInput("SetMessage", worldText, worldText, message);
                    }
                    // let everyone else know
                    SendGlobalChatMessage(Localizer["DicePlayerMakeFakeGunSoundsWeapon_other"].Value
                        .Replace("{playerName}", player.PlayerName)
                        .Replace("{weapon}", weaponName),
                        player: player);
                    // disable fake gun sounds for player
                    _playersWithFakeGunSounds[player] = 0;
                }
                catch (Exception e)
                {
                    // remove player
                    _playersWithFakeGunSounds.Remove(player);
                    // log error
                    Console.WriteLine(Localizer["core.error"].Value.Replace("{error}", e.Message));
                }
            }
        }

        private void EmitFakeGunSounds(nint playerHandle, string soundName, float soundLength, int playTotal, int playCount = 0)
        {
            playCount += 1;
            CCSPlayerController? player = new CCSPlayerController(playerHandle);
            if (player == null
                || player.PlayerPawn == null || !player.PlayerPawn.IsValid || player.PlayerPawn.Value == null
                || player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE
                || !_playersThatRolledTheDice.ContainsKey(player)) return;
            EmitSound(player, soundName);
            if (playCount >= playTotal)
            {
                // reset timer
                Dictionary<string, object> config = GetDiceConfig("DicePlayerMakeFakeGunSounds");
                _playersWithFakeGunSounds[player] = (int)Server.CurrentTime + _random.Next(
                    Convert.ToInt32(config["min_sound_delay"]),
                    Convert.ToInt32(config["max_sound_delay"])
                );
                if (!_playersThatRolledTheDice[player].ContainsKey("gui_status")
                    || (CPointWorldText)_playersThatRolledTheDice[player]["gui_status"] == null) return;
                CPointWorldText worldText = (CPointWorldText)_playersThatRolledTheDice[player]["gui_status"];
                worldText.AcceptInput("SetMessage", worldText, worldText, "");
                return;
            }
            ;
            AddTimer(soundLength, () =>
            {
                if (playerHandle == IntPtr.Zero) return;
                float randomDelay = (float)(_random.NextDouble() * (soundLength / 4)) + (soundLength / 3);
                EmitFakeGunSounds(playerHandle, soundName, randomDelay, playTotal, playCount);
            });
        }

        private Dictionary<string, object> DicePlayerMakeFakeGunSoundsConfig()
        {
            var config = new Dictionary<string, object>();
            config["min_sound_delay"] = (int)5;
            config["max_sound_delay"] = (int)15;
            return config;
        }
    }
}
