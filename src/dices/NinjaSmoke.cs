using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Configs;
using RollTheDice.Utils;
using System.Globalization;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    public class NinjaSmoke : DiceBlueprint
    {
        public override string ClassName => "NinjaSmoke";
        public override List<string> Listeners { get; } = ["OnTick", "OnPlayerButtonsChanged"];

        private sealed class NinjaState
        {
            public int Charges;
            public float FastUntil;
        }

        private readonly Dictionary<CCSPlayerController, NinjaState> _state = [];

        public NinjaSmoke(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            base.Add(player);
            if (_players.Contains(player))
            {
                _state[player] = new NinjaState { Charges = _config.Dices.NinjaSmoke.Charges };
            }
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            base.Remove(player, reason);
            if (_state.Remove(player, out NinjaState? state) && state.FastUntil > 0f)
            {
                EndBoost(player);
            }
        }

        public override void Reset()
        {
            foreach ((CCSPlayerController player, NinjaState state) in _state)
            {
                if (state.FastUntil > 0f)
                {
                    EndBoost(player);
                }
            }
            _state.Clear();
            base.Reset();
        }

        /// <summary>USE (not while defusing): a smoke cloud bursts at the feet at once (a particle cloud, see Particle.Smoke) and the holder runs faster for a moment.</summary>
        public void OnPlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
        {
            if (_state.Count == 0
                || (pressed & PlayerButtons.Use) == 0
                || !_state.TryGetValue(player, out NinjaState? state)
                || state.Charges <= 0
                || !IsAlivePlayer(player)
                || player.PlayerPawn.Value is not { IsValid: true, AbsOrigin: { } origin } pawn
                || pawn.IsDefusing)
            {
                return;
            }

            Particle.Start(Particle.Smoke, new Vector(origin.X, origin.Y, origin.Z + 4f), _config.Dices.NinjaSmoke.SmokeSeconds);
            state.Charges--;
            state.FastUntil = Server.CurrentTime + _config.Dices.NinjaSmoke.BoostSeconds;
            ShowStatus(player, "dice_NinjaSmoke_status", new() { { "charges", state.Charges.ToString(CultureInfo.InvariantCulture) } });
        }

        public void OnTick()
        {
            if (_state.Count == 0)
            {
                return;
            }

            float now = Server.CurrentTime;
            float speed = _config.Dices.NinjaSmoke.SpeedFactor;
            foreach ((CCSPlayerController player, NinjaState state) in _state)
            {
                if (state.FastUntil <= 0f || !IsAlivePlayer(player) || player.PlayerPawn.Value is not { IsValid: true } pawn)
                {
                    continue;
                }
                if (now >= state.FastUntil)
                {
                    state.FastUntil = 0f;
                    EndBoost(player);
                    continue;
                }
                if (pawn.VelocityModifier < speed)
                {
                    pawn.VelocityModifier = speed;
                    Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
                }
            }
        }

        private static void EndBoost(CCSPlayerController player)
        {
            if (player.IsValid && player.PlayerPawn.Value is { IsValid: true } pawn && pawn.VelocityModifier > 1f)
            {
                pawn.VelocityModifier = 1f;
                Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
            }
        }
    }
}

namespace RollTheDice.Configs
{
    public class NinjaSmokeConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("charges")] public int Charges { get; set; } = 2;
        [JsonPropertyName("boost_seconds")] public float BoostSeconds { get; set; } = 2f;
        [JsonPropertyName("speed_factor")] public float SpeedFactor { get; set; } = 1.4f;
        // how long the smoke cloud stays
        [JsonPropertyName("smoke_seconds")] public float SmokeSeconds { get; set; } = 15f;
    }
}
