using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Utils;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    public class Butterfingers : DiceBlueprint
    {
        public override string ClassName => "Butterfingers";
        public override List<string> Events { get; } = ["EventPlayerHurt"];
        private readonly Random _random = new();
        // the gun slips a few meters away, so it is not picked up again at once
        private const float ThrowSpeed = 300f;

        public Butterfingers(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        /// <summary>Hitting someone can make the holder drop the own gun.</summary>
        public HookResult EventPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
        {
            CCSPlayerController? attacker = @event.Attacker;
            CCSPlayerController? victim = @event.Userid;
            if (_players.Count == 0
                || attacker is not { IsValid: true }
                || victim is not { IsValid: true }
                || attacker == victim
                || !_players.Contains(attacker)
                || _random.NextDouble() >= _config.Dices.Butterfingers.Chance)
            {
                return HookResult.Continue;
            }

            Server.NextFrame(() =>
            {
                if (!Targets.IsAlive(attacker) || !Weapons.DropActiveGun(attacker, ThrowSpeed))
                {
                    return;
                }
                Alert(attacker, "dice_Butterfingers_dropped");
            });
            return HookResult.Continue;
        }
    }
}

namespace RollTheDice.Configs
{
    public class ButterfingersConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        // chance per hit (0.0 - 1.0)
        [JsonPropertyName("chance")] public float Chance { get; set; } = 0.15f;
    }
}
