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
    public class Jetpack : DiceBlueprint
    {
        public override string ClassName => "Jetpack";
        public override List<string> Listeners { get; } = ["OnTick"];
        // the fuel bar in the banner has this many segments; it is only redrawn when a segment changes
        private const int BarSegments = 10;

        private sealed class JetpackState
        {
            public float Fuel;
            // ground height of the last take-off: the flight ceiling is measured from there
            public float GroundZ;
            public int ShownSegments = -1;
        }

        private readonly Dictionary<CCSPlayerController, JetpackState> _state = [];

        public Jetpack(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            base.Add(player);
            if (_players.Contains(player))
            {
                _state[player] = new JetpackState { Fuel = _config.Dices.Jetpack.FuelSeconds };
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

        /// <summary>
        /// Holding jump in the air pushes up while fuel lasts (up to a ceiling above the take-off point); on the
        /// ground the fuel refills.
        /// </summary>
        public void OnTick()
        {
            if (_state.Count == 0)
            {
                return;
            }

            JetpackConfig config = _config.Dices.Jetpack;
            float dt = Server.TickInterval;
            foreach ((CCSPlayerController player, JetpackState state) in _state)
            {
                if (!IsAlivePlayer(player) || player.PlayerPawn.Value is not { IsValid: true, AbsOrigin: { } origin } pawn)
                {
                    continue;
                }

                if (Motion.IsOnGround(pawn))
                {
                    state.GroundZ = origin.Z;
                    state.Fuel = Math.Min(config.FuelSeconds, state.Fuel + (config.RechargePerSecond * dt));
                }
                else if ((player.Buttons & PlayerButtons.Jump) != 0 && state.Fuel > 0f)
                {
                    state.Fuel = Math.Max(0f, state.Fuel - dt);
                    Vector v = Motion.Velocity(pawn);
                    // above the ceiling the thrust only holds the height
                    float maxUp = origin.Z - state.GroundZ >= config.MaxHeight ? 0f : config.MaxUpSpeed;
                    if (v.Z < maxUp)
                    {
                        v.Z = Math.Min(maxUp, v.Z + (config.Thrust * dt));
                        Motion.SetVelocity(pawn, v);
                    }
                }

                ShowFuel(player, state, config);
            }
        }

        private void ShowFuel(CCSPlayerController player, JetpackState state, JetpackConfig config)
        {
            int segments = (int)MathF.Ceiling(state.Fuel / Math.Max(0.01f, config.FuelSeconds) * BarSegments);
            if (segments == state.ShownSegments)
            {
                return;
            }
            state.ShownSegments = segments;
            string bar = new string('■', segments) + new string('□', BarSegments - segments);
            ShowStatus(player, "dice_Jetpack_status", new() { { "fuel", bar } });
        }
    }
}

namespace RollTheDice.Configs
{
    public class JetpackConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        // seconds of thrust on a full tank
        [JsonPropertyName("fuel_seconds")] public float FuelSeconds { get; set; } = 3f;
        // upward acceleration in units/s² (gravity pulls with 800)
        [JsonPropertyName("thrust")] public float Thrust { get; set; } = 2000f;
        [JsonPropertyName("max_up_speed")] public float MaxUpSpeed { get; set; } = 320f;
        // fuel seconds regained per second on the ground
        [JsonPropertyName("recharge_per_second")] public float RechargePerSecond { get; set; } = 1f;
        // highest climb above the take-off point in units (a storey is about 128): keeps roofs and the skybox away
        [JsonPropertyName("max_height")] public float MaxHeight { get; set; } = 300f;
    }
}
