using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Hud;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    public class MakeFakeGunSounds : DiceBlueprint
    {
        public override string ClassName => "MakeFakeGunSounds";
        public override List<string> Listeners { get; } = ["OnTick"];

        private readonly Random _random = new();
        private readonly Dictionary<CCSPlayerController, int> _nextSound = [];
        private static readonly (string Weapon, string Sound, int Total, float Length)[] Sounds =
        [
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
        ];

        public MakeFakeGunSounds(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (!IsAlivePlayer(player))
            {
                return;
            }
            _players.Add(player);
            _nextSound[player] = (int)Server.CurrentTime + _random.Next(3, 10);
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _nextSound.Remove(player);
            base.Remove(player, reason);
        }

        public override void Reset()
        {
            _nextSound.Clear();
            base.Reset();
        }

        public void OnTick()
        {
            if (_nextSound.Count == 0)
            {
                return;
            }

            int now = (int)Server.CurrentTime;
            foreach ((CCSPlayerController player, int next) in _nextSound)
            {
                if (next == 0 || next >= now || player.Buttons != 0 || !IsAlivePlayer(player))
                {
                    continue;
                }

                (string weapon, string sound, int total, float length) = Sounds[_random.Next(Sounds.Length)];
                EmitBurst(player.Handle, sound, length, total);
                Banner.Show(player, StripChatColors(
                    _localizer["dice_MakeFakeGunSoundsWeapon_player"].Value.Replace("{weapon}", weapon)));

                if (_globalConfig.NotifyOtherPlayers)
                {
                    LocalizedString otherKey = _localizer["dice_MakeFakeGunSoundsWeapon_other"];
                    if (!otherKey.ResourceNotFound)
                    {
                        string other = otherKey.Value
                            .Replace("{playerName}", player.PlayerName)
                            .Replace("{weapon}", weapon);
                        foreach (CCSPlayerController entry in Utilities.GetPlayers())
                        {
                            if (entry == player || !entry.IsValid || entry.IsBot || entry.IsHLTV)
                            {
                                continue;
                            }
                            entry.PrintToChat(other);
                        }
                    }
                }
                _nextSound[player] = 0;
            }
        }

        private void EmitBurst(nint playerHandle, string soundName, float soundLength, int playTotal, int playCount = 0)
        {
            playCount++;
            CCSPlayerController player = new(playerHandle);
            if (!IsAlivePlayer(player) || !_nextSound.ContainsKey(player))
            {
                return;
            }

            _ = player.EmitSound(soundName);
            if (playCount >= playTotal)
            {
                int min = _config.Dices.MakeFakeGunSounds.MinSoundDelay;
                int max = Math.Max(min + 1, _config.Dices.MakeFakeGunSounds.MaxSoundDelay + 1);
                _nextSound[player] = (int)Server.CurrentTime + _random.Next(min, max);
                return;
            }

            float randomDelay = (float)(_random.NextDouble() * (soundLength / 4)) + (soundLength / 3);
            int count = playCount;
            _ = new CounterStrikeSharp.API.Modules.Timers.Timer(
                randomDelay,
                () => EmitBurst(playerHandle, soundName, randomDelay, playTotal, count),
                TimerFlags.STOP_ON_MAPCHANGE);
        }
    }
}

namespace RollTheDice.Configs
{
    public class MakeFakeGunSoundsConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("min_sound_delay")] public int MinSoundDelay { get; set; } = 5;
        [JsonPropertyName("max_sound_delay")] public int MaxSoundDelay { get; set; } = 15;
    }
}
