using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Configs;
using RollTheDice.Utils;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    public class Disarm : DiceBlueprint
    {
        public override string ClassName => "Disarm";
        public override List<string> Events { get; } = ["EventPlayerHurt"];
        private readonly Random _random = new();
        // the gun flies a few meters, so it is not picked up again at once
        private const float ThrowSpeed = 300f;

        public Disarm(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        /// <summary>A hit can knock the victim's gun out of the hand (knife hits always do). Knife, Zeus, grenades and C4 stay.</summary>
        public HookResult EventPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
        {
            CCSPlayerController? attacker = @event.Attacker;
            CCSPlayerController? victim = @event.Userid;
            if (_players.Count == 0
                || attacker is not { IsValid: true }
                || victim is not { IsValid: true }
                || attacker == victim
                || !_players.Contains(attacker)
                || !Targets.IsEnemy(attacker, victim))
            {
                return HookResult.Continue;
            }

            DisarmConfig config = _config.Dices.Disarm;
            float chance = Weapons.IsKnife(@event.Weapon) ? config.KnifeChance : config.Chance;
            if (_random.NextDouble() >= chance)
            {
                return HookResult.Continue;
            }

            Server.NextFrame(() =>
            {
                if (!Targets.IsAlive(victim) || !Weapons.DropActiveGun(victim, ThrowSpeed))
                {
                    return;
                }
                Tell(victim, "dice_Disarm_victim", new() { { "playerName", attacker.PlayerName } });
                if (attacker.IsValid)
                {
                    Alert(attacker, "dice_Disarm_hit", new() { { "playerName", victim.PlayerName } });
                }
            });
            return HookResult.Continue;
        }
    }
}

namespace RollTheDice.Configs
{
    public class DisarmConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        // chance per hit (0.0 - 1.0)
        [JsonPropertyName("chance")] public float Chance { get; set; } = 0.2f;
        [JsonPropertyName("knife_chance")] public float KnifeChance { get; set; } = 1.0f;
    }
}
