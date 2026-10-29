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
        // the sway fades in over this time instead of jumping to the wave's current value
        private const float FadeInSeconds = 1f;
        private readonly Random _random = new();
        private readonly Dictionary<CCSPlayerController, Sway> _sways = [];

        /// <summary>Random wave phases per player.</summary>
        private sealed class Sway
        {
            public required float[] Phases;
            public float Start;
            public float NextStumble;
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
                float now = Server.CurrentTime;
                _sways[player] = new Sway { Phases = phases, Start = now, NextStumble = now + 1f };
            }
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            base.Remove(player, reason);
            if (_sways.Remove(player))
            {
                ClearPunch(player);
            }
        }

        public override void Reset()
        {
            foreach (CCSPlayerController player in _sways.Keys)
            {
                ClearPunch(player);
            }
            base.Reset();
            _sways.Clear();
        }

        /// <summary>
        /// The camera drifts on two slow overlapping waves and tilts with them. The sway is the view punch of the camera
        /// (an offset on top of the view), never the eye angles: setting those every tick took the mouse away, because
        /// each server write pulled the view back to where the player had aimed a moment earlier. Now and then a soft
        /// stumble to the side the view leans to.
        /// </summary>
        public void OnTick()
        {
            if (_sways.Count == 0)
            {
                return;
            }

            DrunkConfig config = _config.Dices.Drunk;
            float now = Server.CurrentTime;
            int tick = Server.TickCount;
            // one full sway takes sway_seconds; the second wave is faster and weaker, so the pattern never repeats
            float speed = MathF.Tau / Math.Max(0.5f, config.SwaySeconds);
            foreach ((CCSPlayerController player, Sway sway) in _sways)
            {
                if (!IsAlivePlayer(player)
                    || player.PlayerPawn.Value is not { IsValid: true } pawn
                    || pawn.CameraServices is not { } camera)
                {
                    continue;
                }

                float[] phase = sway.Phases;
                float amount = config.SwayDegrees * Math.Min(1f, (now - sway.Start) / FadeInSeconds);
                float yaw = amount * ((0.7f * MathF.Sin((now * speed) + phase[0])) + (0.3f * MathF.Sin((now * speed * 2.3f) + phase[1])));
                float pitch = amount * 0.5f * ((0.7f * MathF.Sin((now * speed * 0.8f) + phase[2])) + (0.3f * MathF.Sin((now * speed * 1.9f) + phase[3])));
                SetPunch(pawn, camera, pitch, yaw, yaw * config.TiltShare, tick);

                if (now < sway.NextStumble)
                {
                    continue;
                }
                sway.NextStumble = now + 1f;
                if (Motion.IsOnGround(pawn) && _random.NextDouble() < config.StumbleChance)
                {
                    // the stumble goes to the side the view leans to
                    float angle = (pawn.EyeAngles.Y + (yaw >= 0f ? 90f : -90f)) * MathF.PI / 180f;
                    Vector v = Motion.Velocity(pawn);
                    Motion.SetVelocity(pawn, new Vector(v.X + (MathF.Cos(angle) * config.StumbleSpeed), v.Y + (MathF.Sin(angle) * config.StumbleSpeed), v.Z));
                }
            }
        }

        private static void SetPunch(CCSPlayerPawn pawn, CPlayer_CameraServices camera, float pitch, float yaw, float roll, int tick)
        {
            camera.CsViewPunchAngle.X = pitch;
            camera.CsViewPunchAngle.Y = yaw;
            camera.CsViewPunchAngle.Z = roll;
            camera.CsViewPunchAngleTick = tick;
            camera.CsViewPunchAngleTickRatio = 0f;
            Utilities.SetStateChanged(pawn, "CBasePlayerPawn", "m_pCameraServices");
        }

        private static void ClearPunch(CCSPlayerController player)
        {
            if (player.IsValid
                && player.PlayerPawn.Value is { IsValid: true } pawn
                && pawn.CameraServices is { } camera)
            {
                SetPunch(pawn, camera, 0f, 0f, 0f, Server.TickCount);
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
        // the camera also tilts sideways by this share of the sideways sway (0 = no tilt)
        [JsonPropertyName("tilt_share")] public float TiltShare { get; set; } = 0.6f;
        // chance per second to stumble sideways (0.0 - 1.0)
        [JsonPropertyName("stumble_chance")] public float StumbleChance { get; set; } = 0.25f;
        [JsonPropertyName("stumble_speed")] public float StumbleSpeed { get; set; } = 120f;
    }
}
