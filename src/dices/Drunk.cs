using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Configs;
using RollTheDice.Utils;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    public class Drunk : DiceBlueprint
    {
        public override string ClassName => "Drunk";
        public override List<string> Listeners { get; } = ["OnTick"];
        private readonly Random _random = new();
        private readonly Dictionary<CCSPlayerController, float> _nextSway = [];

        public Drunk(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            base.Add(player);
            if (_players.Contains(player))
            {
                _nextSway[player] = 0f;
            }
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            base.Remove(player, reason);
            _ = _nextSway.Remove(player);
        }

        public override void Reset()
        {
            base.Reset();
            _nextSway.Clear();
        }

        /// <summary>Every few tenths of a second the view tilts a little; now and then a stumble to the side.</summary>
        public void OnTick()
        {
            if (_nextSway.Count == 0)
            {
                return;
            }

            DrunkConfig config = _config.Dices.Drunk;
            float now = Server.CurrentTime;
            foreach ((CCSPlayerController player, float next) in _nextSway)
            {
                if (now < next
                    || !IsAlivePlayer(player)
                    || player.PlayerPawn.Value is not { IsValid: true } pawn)
                {
                    continue;
                }

                _nextSway[player] = now + config.MinIntervalSeconds + ((float)_random.NextDouble() * Math.Max(0f, config.MaxIntervalSeconds - config.MinIntervalSeconds));
                QAngle eyes = pawn.EyeAngles;
                QAngle sway = new(
                    Math.Clamp(eyes.X + Spread(config.SwayDegrees), -89f, 89f),
                    eyes.Y + Spread(config.SwayDegrees * 1.5f),
                    0f);

                Vector? stumble = null;
                if (Motion.IsOnGround(pawn) && _random.NextDouble() < config.StumbleChance)
                {
                    float yaw = (eyes.Y + (_random.Next(2) == 0 ? 90f : -90f)) * MathF.PI / 180f;
                    Vector v = Motion.Velocity(pawn);
                    stumble = new Vector(v.X + (MathF.Cos(yaw) * config.StumbleSpeed), v.Y + (MathF.Sin(yaw) * config.StumbleSpeed), v.Z);
                }
                pawn.Teleport(null, sway, stumble);
            }
        }

        private float Spread(float max) => ((float)_random.NextDouble() * 2f - 1f) * max;
    }
}

namespace RollTheDice.Configs
{
    public class DrunkConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("sway_degrees")] public float SwayDegrees { get; set; } = 2.5f;
        [JsonPropertyName("min_interval_seconds")] public float MinIntervalSeconds { get; set; } = 0.3f;
        [JsonPropertyName("max_interval_seconds")] public float MaxIntervalSeconds { get; set; } = 0.6f;
        // chance per sway to stumble sideways (0.0 - 1.0)
        [JsonPropertyName("stumble_chance")] public float StumbleChance { get; set; } = 0.15f;
        [JsonPropertyName("stumble_speed")] public float StumbleSpeed { get; set; } = 180f;
    }
}
