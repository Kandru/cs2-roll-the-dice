using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Hud;
using RollTheDice.Configs;
using RollTheDice.Utils;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    public class Airstrike : DiceBlueprint
    {
        public override string ClassName => "Airstrike";
        public override List<string> Events { get; } = ["EventSmokegrenadeDetonate"];
        public override List<string> Listeners { get; } = ["OnTick"];
        private readonly Random _random = new();

        // a marked target: the warning beeps once per second until the strike
        private sealed class Strike(Vector center, float hitAt)
        {
            public readonly Vector Center = center;
            public readonly float HitAt = hitAt;
            public float NextBeepAt;
        }

        private readonly List<Strike> _strikes = [];
        private readonly Dictionary<CCSPlayerController, int> _strikesLeft = [];

        public Airstrike(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            base.Add(player);
            if (_players.Contains(player))
            {
                _strikesLeft[player] = _config.Dices.Airstrike.Strikes;
                Weapons.GiveOnce(player, "weapon_smokegrenade");
            }
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            base.Remove(player, reason);
            _ = _strikesLeft.Remove(player);
        }

        public override void Reset()
        {
            base.Reset();
            _strikes.Clear();
            _strikesLeft.Clear();
        }

        /// <summary>
        /// The holder's smoke marks the target and smokes as usual: the explosions are queued at once (entity I/O),
        /// the warning beeps until they hit.
        /// </summary>
        public HookResult EventSmokegrenadeDetonate(EventSmokegrenadeDetonate @event, GameEventInfo info)
        {
            if (_strikesLeft.Count == 0
                || @event.Userid is not { IsValid: true } player
                || !_strikesLeft.TryGetValue(player, out int left)
                || left <= 0)
            {
                return HookResult.Continue;
            }

            AirstrikeConfig config = _config.Dices.Airstrike;
            Vector center = new(@event.X, @event.Y, @event.Z);
            float now = Server.CurrentTime;
            CCSPlayerPawn? pawn = player.PlayerPawn.Value is { IsValid: true } p ? p : null;
            for (int i = 0; i < config.Bombs; i++)
            {
                // spread over half a second, so the hits roll in one after another; a grenade shows each spot until then
                float delay = config.DelaySeconds + (i * 0.5f / Math.Max(1, config.Bombs - 1));
                Explosion.Detonate(Explosion.Scatter(center, config.Spread, _random), pawn, delay, config.Damage, config.Radius);
            }
            _strikes.Add(new Strike(center, now + config.DelaySeconds) { NextBeepAt = now });

            _strikesLeft[player] = --left;
            Tell(player, "dice_Airstrike_called");
            if (left > 0)
            {
                Weapons.GiveOnce(player, "weapon_smokegrenade");
            }
            return HookResult.Continue;
        }

        /// <summary>Warning beep for everyone near a marked target, once per second.</summary>
        public void OnTick()
        {
            if (_strikes.Count == 0)
            {
                return;
            }

            AirstrikeConfig config = _config.Dices.Airstrike;
            float now = Server.CurrentTime;
            for (int i = _strikes.Count - 1; i >= 0; i--)
            {
                Strike strike = _strikes[i];
                if (now >= strike.HitAt)
                {
                    _strikes.RemoveAt(i);
                    continue;
                }
                if (now < strike.NextBeepAt || string.IsNullOrEmpty(config.WarningSound))
                {
                    continue;
                }

                strike.NextBeepAt = now + 1f;
                foreach ((CCSPlayerController player, CCSPlayerPawn _, float _) in Targets.Around(strike.Center, config.WarningRange))
                {
                    if (!player.IsBot)
                    {
                        Reel.PlaySound(player, config.WarningSound);
                    }
                }
            }
        }
    }
}

namespace RollTheDice.Configs
{
    public class AirstrikeConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        // marking smokes per round (a new smoke is given after each strike)
        [JsonPropertyName("strikes")] public int Strikes { get; set; } = 1;
        [JsonPropertyName("delay_seconds")] public float DelaySeconds { get; set; } = 3f;
        [JsonPropertyName("bombs")] public int Bombs { get; set; } = 5;
        // the hits land up to this far from the mark
        [JsonPropertyName("spread")] public float Spread { get; set; } = 250f;
        // per bomb (a normal HE deals 99 within 350)
        [JsonPropertyName("damage")] public float Damage { get; set; } = 80f;
        [JsonPropertyName("radius")] public float Radius { get; set; } = 250f;
        // heard by every player this close to the mark (sound event or sounds/... path, empty = off)
        [JsonPropertyName("warning_sound")] public string WarningSound { get; set; } = "sounds/weapons/c4/c4_beep2.vsnd";
        [JsonPropertyName("warning_range")] public float WarningRange { get; set; } = 1500f;
    }
}
