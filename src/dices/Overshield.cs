using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Hud;
using RollTheDice.Configs;
using RollTheDice.Utils;
using System.Globalization;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    public class Overshield : DiceBlueprint
    {
        public override string ClassName => "Overshield";
        public override List<string> Listeners { get; } = ["OnTick", "OnPlayerTakeDamagePre"];

        private sealed class ShieldState
        {
            public float Points;
            public float LastHitAt;
            public int ShownPoints = -1;
        }

        private readonly Dictionary<CCSPlayerController, ShieldState> _state = [];

        public Overshield(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            base.Add(player);
            if (_players.Contains(player))
            {
                ShieldState state = new() { Points = _config.Dices.Overshield.Points };
                _state[player] = state;
                Show(player, state);
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

        /// <summary>Damage goes to the shield first; only the rest reaches health and armor.</summary>
        public HookResult OnPlayerTakeDamagePre(CCSPlayerPawn pawn, CTakeDamageInfo info)
        {
            if (_state.Count == 0
                || info.Damage <= 0f
                || HolderOf(pawn) is not { } player
                || !_state.TryGetValue(player, out ShieldState? state))
            {
                return HookResult.Continue;
            }

            state.LastHitAt = Server.CurrentTime;
            if (state.Points <= 0f)
            {
                return HookResult.Continue;
            }

            float absorbed = Math.Min(state.Points, info.Damage);
            state.Points -= absorbed;
            info.Damage -= absorbed;
            Show(player, state);
            if (state.Points <= 0f)
            {
                Alert(player, "dice_Overshield_broken");
                if (!string.IsNullOrEmpty(_config.Dices.Overshield.BreakSound))
                {
                    Reel.PlaySound(player, _config.Dices.Overshield.BreakSound);
                }
            }
            return info.Damage <= 0f ? HookResult.Stop : HookResult.Changed;
        }

        /// <summary>After a few seconds without damage the shield fills up again.</summary>
        public void OnTick()
        {
            if (_state.Count == 0 || Server.TickCount % 4 != 0)
            {
                return;
            }

            OvershieldConfig config = _config.Dices.Overshield;
            float now = Server.CurrentTime;
            foreach ((CCSPlayerController player, ShieldState state) in _state)
            {
                if (state.Points >= config.Points || now - state.LastHitAt < config.RegenDelaySeconds || !IsAlivePlayer(player))
                {
                    continue;
                }
                state.Points = Math.Min(config.Points, state.Points + (config.RegenPerSecond * Server.TickInterval * 4));
                Show(player, state);
            }
        }

        private void Show(CCSPlayerController player, ShieldState state)
        {
            int points = (int)MathF.Ceiling(state.Points);
            if (points == state.ShownPoints)
            {
                return;
            }
            state.ShownPoints = points;
            ShowStatus(player, "dice_Overshield_status", new() { { "points", points.ToString(CultureInfo.InvariantCulture) } });
        }
    }
}

namespace RollTheDice.Configs
{
    public class OvershieldConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("points")] public float Points { get; set; } = 50f;
        [JsonPropertyName("regen_delay_seconds")] public float RegenDelaySeconds { get; set; } = 4f;
        [JsonPropertyName("regen_per_second")] public float RegenPerSecond { get; set; } = 10f;
        // played to the holder when the shield breaks (sound event or sounds/... path, empty = off)
        [JsonPropertyName("break_sound")] public string BreakSound { get; set; } = "sounds/weapons/clipempty_rifle.vsnd";
    }
}
