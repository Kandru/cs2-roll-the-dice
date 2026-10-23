using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Configs;
using RollTheDice.Utils;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    public class ClusterGrenade : DiceBlueprint
    {
        public override string ClassName => "ClusterGrenade";
        public override List<string> Events { get; } = ["EventHegrenadeDetonate"];
        private readonly Random _random = new();

        public ClusterGrenade(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            base.Add(player);
            if (_players.Contains(player))
            {
                Weapons.GiveOnce(player, "weapon_hegrenade");
            }
        }

        /// <summary>
        /// The holder's HE splits into bomblets around the blast that go off a moment later. The bomblets are
        /// env_explosions, not grenades, so they never split again.
        /// </summary>
        public HookResult EventHegrenadeDetonate(EventHegrenadeDetonate @event, GameEventInfo info)
        {
            if (_players.Count == 0 || @event.Userid is not { IsValid: true } player || !_players.Contains(player))
            {
                return HookResult.Continue;
            }

            ClusterGrenadeConfig config = _config.Dices.ClusterGrenade;
            Vector center = new(@event.X, @event.Y, @event.Z);
            CCSPlayerPawn? pawn = player.PlayerPawn.Value is { IsValid: true } p ? p : null;
            for (int i = 0; i < config.Bomblets; i++)
            {
                Explosion.Detonate(Explosion.Scatter(center, config.Spread, _random), pawn, config.DelaySeconds, config.Damage, config.Radius);
            }
            Debug($"{player.PlayerName} cluster grenade: {config.Bomblets} bomblets");
            return HookResult.Continue;
        }
    }
}

namespace RollTheDice.Configs
{
    public class ClusterGrenadeConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("bomblets")] public int Bomblets { get; set; } = 4;
        // bomblets land up to this far from the blast
        [JsonPropertyName("spread")] public float Spread { get; set; } = 250f;
        [JsonPropertyName("delay_seconds")] public float DelaySeconds { get; set; } = 1f;
        // per bomblet (a normal HE deals 99 within 350)
        [JsonPropertyName("damage")] public float Damage { get; set; } = 50f;
        [JsonPropertyName("radius")] public float Radius { get; set; } = 220f;
    }
}
