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
    public class Slide : DiceBlueprint
    {
        public override string ClassName => "Slide";
        public override List<string> Listeners { get; } = ["OnTick", "OnPlayerButtonsChanged"];

        private sealed class SlideState
        {
            public float ReadyAt;
            public float StartedAt;
            public float EndsAt;
            public float DirX;
            public float DirY;
            public float StartSpeed;
            public float EndSpeed;
        }

        private readonly Dictionary<CCSPlayerController, SlideState> _state = [];

        public Slide(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            base.Add(player);
            if (_players.Contains(player))
            {
                _state[player] = new SlideState();
            }
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            base.Remove(player, reason);
            _ = _state.Remove(player);
        }

        public override void Reset()
        {
            base.Reset();
            _state.Clear();
        }

        /// <summary>Crouching while running fast starts a slide along the running direction, then the cooldown.</summary>
        public void OnPlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
        {
            if (_state.Count == 0
                || (pressed & PlayerButtons.Duck) == 0
                || !_state.TryGetValue(player, out SlideState? state)
                || Server.CurrentTime < state.ReadyAt
                || !IsAlivePlayer(player)
                || player.PlayerPawn.Value is not { IsValid: true } pawn
                || !Motion.IsOnGround(pawn))
            {
                return;
            }

            SlideConfig config = _config.Dices.Slide;
            float speed = Motion.HorizontalSpeed(pawn);
            if (speed < config.MinSpeed)
            {
                return;
            }

            Vector v = Motion.Velocity(pawn);
            float now = Server.CurrentTime;
            state.DirX = v.X / speed;
            state.DirY = v.Y / speed;
            state.StartSpeed = Math.Min(speed * config.SpeedFactor, config.MaxSpeed);
            state.EndSpeed = speed;
            state.StartedAt = now;
            state.EndsAt = now + config.SlideSeconds;
            state.ReadyAt = now + config.CooldownSeconds;
            Debug($"{player.PlayerName} slides from {speed:0} to {state.StartSpeed:0}");
        }

        /// <summary>
        /// While a slide lasts the crouch friction is overridden: the speed holds near the burst and falls back to the
        /// run towards the end (quadratic ease).
        /// </summary>
        public void OnTick()
        {
            if (_state.Count == 0)
            {
                return;
            }

            float now = Server.CurrentTime;
            foreach ((CCSPlayerController player, SlideState state) in _state)
            {
                if (now >= state.EndsAt
                    || !IsAlivePlayer(player)
                    || player.PlayerPawn.Value is not { IsValid: true } pawn
                    || !Motion.IsOnGround(pawn))
                {
                    continue;
                }

                float t = (now - state.StartedAt) / Math.Max(0.01f, state.EndsAt - state.StartedAt);
                float speed = state.StartSpeed + ((state.EndSpeed - state.StartSpeed) * t * t);
                Vector v = Motion.Velocity(pawn);
                v.X = state.DirX * speed;
                v.Y = state.DirY * speed;
                Motion.SetVelocity(pawn, v);
            }
        }
    }
}

namespace RollTheDice.Configs
{
    public class SlideConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        // running speed needed to start a slide (a knife run is about 250)
        [JsonPropertyName("min_speed")] public float MinSpeed { get; set; } = 200f;
        [JsonPropertyName("speed_factor")] public float SpeedFactor { get; set; } = 2.4f;
        [JsonPropertyName("max_speed")] public float MaxSpeed { get; set; } = 750f;
        [JsonPropertyName("slide_seconds")] public float SlideSeconds { get; set; } = 1f;
        [JsonPropertyName("cooldown_seconds")] public float CooldownSeconds { get; set; } = 2f;
    }
}
