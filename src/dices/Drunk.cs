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
        private readonly Dictionary<CCSPlayerController, Sway> _sways = [];

        /// <summary>Random wave phases per player and the offset already applied to the view.</summary>
        private sealed class Sway
        {
            public required float[] Phases;
            public float Pitch;
            public float Yaw;
            public float NextStumble;
            public bool Started;
        }

        public Drunk(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            base.Add(player);
            if (_players.Contains(player))
            {
                float[] phases = new float[4];
                for (int i = 0; i < phases.Length; i++)
                {
                    phases[i] = (float)_random.NextDouble() * MathF.Tau;
                }
                _sways[player] = new Sway { Phases = phases, NextStumble = Server.CurrentTime + 1f };
            }
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            base.Remove(player, reason);
            _ = _sways.Remove(player);
        }

        public override void Reset()
        {
            base.Reset();
            _sways.Clear();
        }

        /// <summary>
        /// The view drifts on two slow overlapping waves. Every tick adds only the change of the wave since the last
        /// tick, so the player's own aim stays and the motion has no jumps. Now and then a soft stumble to the side
        /// the view leans to.
        /// </summary>
        public void OnTick()
        {
            if (_sways.Count == 0)
            {
                return;
            }

            DrunkConfig config = _config.Dices.Drunk;
            float now = Server.CurrentTime;
            // one full sway takes sway_seconds; the second wave is faster and weaker, so the pattern never repeats
            float speed = MathF.Tau / Math.Max(0.5f, config.SwaySeconds);
            foreach ((CCSPlayerController player, Sway sway) in _sways)
            {
                if (!IsAlivePlayer(player)
                    || player.PlayerPawn.Value is not { IsValid: true } pawn)
                {
                    continue;
                }

                float[] phase = sway.Phases;
                float yaw = config.SwayDegrees * ((0.7f * MathF.Sin((now * speed) + phase[0])) + (0.3f * MathF.Sin((now * speed * 2.3f) + phase[1])));
                float pitch = config.SwayDegrees * 0.5f * ((0.7f * MathF.Sin((now * speed * 0.8f) + phase[2])) + (0.3f * MathF.Sin((now * speed * 1.9f) + phase[3])));
                if (!sway.Started)
                {
                    // the waves start wherever they are: only their changes move the view
                    sway.Started = true;
                    sway.Pitch = pitch;
                    sway.Yaw = yaw;
                    continue;
                }

                QAngle eyes = pawn.EyeAngles;
                QAngle view = new(
                    Math.Clamp(eyes.X + pitch - sway.Pitch, -89f, 89f),
                    eyes.Y + yaw - sway.Yaw,
                    0f);
                // leaning right while the yaw wave swings right: the stumble follows it
                float lean = yaw - sway.Yaw;
                sway.Pitch = pitch;
                sway.Yaw = yaw;

                Vector? stumble = null;
                if (now >= sway.NextStumble)
                {
                    sway.NextStumble = now + 1f;
                    if (Motion.IsOnGround(pawn) && _random.NextDouble() < config.StumbleChance)
                    {
                        float angle = (eyes.Y + (lean >= 0f ? 90f : -90f)) * MathF.PI / 180f;
                        Vector v = Motion.Velocity(pawn);
                        stumble = new Vector(v.X + (MathF.Cos(angle) * config.StumbleSpeed), v.Y + (MathF.Sin(angle) * config.StumbleSpeed), v.Z);
                    }
                }
                pawn.Teleport(null, view, stumble);
            }
        }
    }
}

namespace RollTheDice.Configs
{
    public class DrunkConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        // how far the view drifts sideways at most (up and down: half of it)
        [JsonPropertyName("sway_degrees")] public float SwayDegrees { get; set; } = 4f;
        // length of one slow sway from one side to the other and back
        [JsonPropertyName("sway_seconds")] public float SwaySeconds { get; set; } = 3f;
        // chance per second to stumble sideways (0.0 - 1.0)
        [JsonPropertyName("stumble_chance")] public float StumbleChance { get; set; } = 0.25f;
        [JsonPropertyName("stumble_speed")] public float StumbleSpeed { get; set; } = 120f;
    }
}
