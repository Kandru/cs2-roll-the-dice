using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;

namespace RollTheDice.Dices
{
    public class LoudSteps : DiceBlueprint
    {
        public override string ClassName => "LoudSteps";
        public readonly Random _random = new();
        public override List<string> Listeners => [
            "OnTick",
            "OnPlayerButtonsChanged"
        ];
        public readonly List<CCSPlayerController> _playersWithLoudSteps = [];

        public LoudSteps(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public void OnTick()
        {
            if (_players.Count == 0
                || _playersWithLoudSteps.Count == 0
                || Server.TickCount % 48 != 0)
            {
                return;
            }
            foreach (CCSPlayerController entry in _playersWithLoudSteps)
            {
                _ = entry.EmitSound(_config.Dices.LoudSteps.SoundEventName);
            }
        }

        public void OnPlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
        {
            if (!_players.Contains(player))
            {
                return;
            }
            if (pressed.HasFlag(PlayerButtons.Duck)
                || pressed.HasFlag(PlayerButtons.Speed))
            {
                if (!_playersWithLoudSteps.Contains(player))
                {
                    _playersWithLoudSteps.Add(player);
                }
            }

            if (!player.Buttons.HasFlag(PlayerButtons.Duck)
                && !player.Buttons.HasFlag(PlayerButtons.Speed)
                && _playersWithLoudSteps.Contains(player))
            {
                _ = _playersWithLoudSteps.Remove(player);
            }
        }
    }
}
