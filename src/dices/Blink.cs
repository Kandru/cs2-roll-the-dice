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
    public class Blink : DiceBlueprint
    {
        public override string ClassName => "Blink";
        public override List<string> Listeners { get; } = ["OnTick", "OnPlayerButtonsChanged"];
        // the blink stops this far in front of a wall
        private const float WallGap = 32f;

        private sealed class BlinkState
        {
            public int Charges;
            // time the next charge comes back (charges return one after another)
            public float NextChargeAt;
        }

        private readonly Dictionary<CCSPlayerController, BlinkState> _state = [];

        public Blink(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            base.Add(player);
            if (_players.Contains(player))
            {
                _state[player] = new BlinkState { Charges = _config.Dices.Blink.Charges };
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

        /// <summary>USE (not while defusing): a short teleport along the view, stopped by walls; the running speed stays.</summary>
        public void OnPlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
        {
            if (_state.Count == 0
                || (pressed & PlayerButtons.Use) == 0
                || !_state.TryGetValue(player, out BlinkState? state)
                || state.Charges <= 0
                || !IsAlivePlayer(player)
                || player.PlayerPawn.Value is not { IsValid: true } pawn
                || pawn.IsDefusing)
            {
                return;
            }

            BlinkConfig config = _config.Dices.Blink;
            Vector eyes = Targets.Eyes(pawn);
            Vector forward = Entities.GetForwardVector(pawn.EyeAngles);
            Vector end = new(eyes.X + (forward.X * config.Distance), eyes.Y + (forward.Y * config.Distance), eyes.Z + (forward.Z * config.Distance));
            TraceResult trace = RayTrace.TraceLine(eyes, end, pawn);
            float reach = trace.DidHit() ? Math.Max(0f, (trace.Fraction * config.Distance) - WallGap) : config.Distance;
            if (reach < WallGap)
            {
                return;
            }

            // the trace runs at eye height: the feet stand on the floor below that point (a look down would put them
            // into the ground), or one eye height lower in the air
            Vector point = new(eyes.X + (forward.X * reach), eyes.Y + (forward.Y * reach), eyes.Z + (forward.Z * reach));
            TraceResult floor = RayTrace.TraceLine(point, new Vector(point.X, point.Y, point.Z - Targets.EyeHeight - 8f), pawn);
            Vector target = floor.DidHit() ? floor.EndPos : new Vector(point.X, point.Y, point.Z - Targets.EyeHeight);
            Vector velocity = Motion.Velocity(pawn);
            if (!SafeTeleport.TryTeleport(pawn, target))
            {
                Debug($"{player.PlayerName} blink blocked at {target}: {SafeTeleport.LastRefusal}");
                Alert(player, "dice_Blink_blocked");
                return;
            }
            Motion.SetVelocity(pawn, new Vector(velocity.X, velocity.Y, Math.Max(0f, velocity.Z)));

            if (state.Charges == config.Charges)
            {
                state.NextChargeAt = Server.CurrentTime + config.RechargeSeconds;
            }
            state.Charges--;
            ShowCharges(player, state);
        }

        /// <summary>Gives charges back one by one.</summary>
        public void OnTick()
        {
            if (_state.Count == 0 || Server.TickCount % 8 != 0)
            {
                return;
            }

            BlinkConfig config = _config.Dices.Blink;
            float now = Server.CurrentTime;
            foreach ((CCSPlayerController player, BlinkState state) in _state)
            {
                if (state.Charges >= config.Charges || now < state.NextChargeAt)
                {
                    continue;
                }
                state.Charges++;
                state.NextChargeAt = now + config.RechargeSeconds;
                ShowCharges(player, state);
            }
        }

        private void ShowCharges(CCSPlayerController player, BlinkState state)
        {
            ShowStatus(player, "dice_Blink_status", new()
            {
                { "charges", state.Charges.ToString(CultureInfo.InvariantCulture) },
                { "max", _config.Dices.Blink.Charges.ToString(CultureInfo.InvariantCulture) },
            });
        }
    }
}

namespace RollTheDice.Configs
{
    public class BlinkConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        // teleport distance in units (about 10 m)
        [JsonPropertyName("distance")] public float Distance { get; set; } = 400f;
        [JsonPropertyName("charges")] public int Charges { get; set; } = 3;
        // seconds per charge coming back
        [JsonPropertyName("recharge_seconds")] public float RechargeSeconds { get; set; } = 6f;
    }
}
