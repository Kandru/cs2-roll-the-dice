using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    public class MakeHostageSounds : DiceBlueprint
    {
        public override string ClassName => "MakeHostageSounds";
        public override List<string> Listeners { get; } = ["OnTick"];

        private const string Sound = "Hostage.Pain";
        private readonly Random _random = new();
        private readonly Dictionary<CCSPlayerController, (int Cooldown, int Delay)> _state = [];

        public MakeHostageSounds(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
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
            _state[player] = (0, 0);
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _state.Remove(player);
            base.Remove(player, reason);
        }

        public override void Reset()
        {
            _state.Clear();
            base.Reset();
        }

        public void OnTick()
        {
            if (_state.Count == 0)
            {
                return;
            }

            int now = (int)Server.CurrentTime;
            int min = _config.Dices.MakeHostageSounds.MinSoundDelay;
            int max = Math.Max(min + 1, _config.Dices.MakeHostageSounds.MaxSoundDelay + 1);

            foreach ((CCSPlayerController player, (int cooldown, int delay)) in _state)
            {
                if (!IsAlivePlayer(player))
                {
                    continue;
                }

                bool idleReady = player.Buttons == 0 && cooldown > -1 && cooldown <= now;
                if (idleReady || delay <= now)
                {
                    _ = player.EmitSound(Sound);
                    _state[player] = (-1, now + _random.Next(min, max));
                    continue;
                }

                if (player.Buttons != 0 && cooldown == -1)
                {
                    _state[player] = (now, delay);
                }
            }
        }
    }
}

namespace RollTheDice.Configs
{
    public class MakeHostageSoundsConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("min_sound_delay")] public int MinSoundDelay { get; set; } = 5;
        [JsonPropertyName("max_sound_delay")] public int MaxSoundDelay { get; set; } = 10;
    }
}
