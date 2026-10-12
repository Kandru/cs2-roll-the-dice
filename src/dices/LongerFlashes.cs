using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    public class LongerFlashes : DiceBlueprint
    {
        public override string ClassName => "LongerFlashes";
        public readonly Random _random = new();
        public override List<string> Events => [
            "EventPlayerBlind",
        ];

        public LongerFlashes(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public HookResult EventPlayerBlind(EventPlayerBlind @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            CCSPlayerController? attacker = @event.Attacker;
            if (player?.IsValid != true
                || attacker?.IsValid != true
                || !_players.Contains(attacker)
                || player.PlayerPawn.Value == null)
            {
                return HookResult.Continue;
            }
            float min = _config.Dices.LongerFlashes.MinBlinddurationFactor;
            float max = _config.Dices.LongerFlashes.MaxBlinddurationFactor;
            float blindDurationFactor = (float)((_random.NextDouble() * (max - min)) + min);
            @event.BlindDuration = (float)(@event.BlindDuration * blindDurationFactor);
            player.PlayerPawn.Value.FlashDuration = @event.BlindDuration;
            Utilities.SetStateChanged(player.PlayerPawn.Value, "CCSPlayerPawnBase", "m_flFlashDuration");
            player.PlayerPawn.Value.BlindUntilTime = Server.CurrentTime + player.PlayerPawn.Value.FlashDuration;
            return HookResult.Continue;
        }
    }
}

namespace RollTheDice.Configs
{
    public class LongerFlashesConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("min_blindduration_factor")] public float MinBlinddurationFactor { get; set; } = 1.3f;
        [JsonPropertyName("max_blindduration_factor")] public float MaxBlinddurationFactor { get; set; } = 3.0f;
    }
}
