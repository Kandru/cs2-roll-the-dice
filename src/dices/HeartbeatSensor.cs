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
    public class HeartbeatSensor : DiceBlueprint
    {
        public override string ClassName => "HeartbeatSensor";
        public override List<string> Listeners { get; } = ["OnTick"];
        // nearest enemy is looked up twice per second; the beep itself is timed per tick
        private const float ScanSeconds = 0.5f;
        private const float UnitsPerMeter = 39.37f;
        private float _nextScan;
        // distance of the nearest enemy (-1 = none in range), next beep
        private readonly Dictionary<CCSPlayerController, (float Distance, float NextBeep)> _state = [];

        public HeartbeatSensor(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            base.Add(player);
            if (_players.Contains(player))
            {
                _state[player] = (-1f, 0f);
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
            _nextScan = 0f;
        }

        /// <summary>Distance to the nearest living enemy in the banner (no direction) and a beep that speeds up when close.</summary>
        public void OnTick()
        {
            if (_state.Count == 0)
            {
                return;
            }

            HeartbeatSensorConfig config = _config.Dices.HeartbeatSensor;
            float now = Server.CurrentTime;
            bool scan = now >= _nextScan;
            if (scan)
            {
                _nextScan = now + ScanSeconds;
            }

            foreach ((CCSPlayerController player, (float lastDistance, float lastBeep)) in _state)
            {
                float distance = lastDistance;
                float nextBeep = lastBeep;
                if (!IsAlivePlayer(player) || player.PlayerPawn.Value is not { IsValid: true, AbsOrigin: { } origin })
                {
                    continue;
                }

                if (scan)
                {
                    distance = Nearest(player, origin, config.Range);
                    if (distance < 0f)
                    {
                        ShowStatus(player, "dice_HeartbeatSensor_none");
                    }
                    else
                    {
                        string meters = ((int)MathF.Round(distance / UnitsPerMeter)).ToString(CultureInfo.InvariantCulture);
                        ShowStatus(player, "dice_HeartbeatSensor_status", new() { { "distance", meters } });
                    }
                }

                if (distance >= 0f && now >= nextBeep)
                {
                    // 0.15 s right next to an enemy, 1.5 s at the edge of the range
                    nextBeep = now + 0.15f + (1.35f * distance / config.Range);
                    if (!string.IsNullOrEmpty(config.Sound))
                    {
                        Reel.PlaySound(player, config.Sound);
                    }
                }
                _state[player] = (distance, nextBeep);
            }
        }

        private static float Nearest(CCSPlayerController player, CounterStrikeSharp.API.Modules.Utils.Vector origin, float range)
        {
            float best = -1f;
            foreach ((CCSPlayerController other, CCSPlayerPawn _, float distance) in Targets.Around(origin, range))
            {
                if (Targets.IsEnemy(player, other) && (best < 0f || distance < best))
                {
                    best = distance;
                }
            }
            return best;
        }
    }
}

namespace RollTheDice.Configs
{
    public class HeartbeatSensorConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        // enemies farther away are not sensed (units, about 50 m)
        [JsonPropertyName("range")] public float Range { get; set; } = 2000f;
        // beep per heartbeat (sound event or sounds/... path, empty = off)
        [JsonPropertyName("sound")] public string Sound { get; set; } = "UIPanorama.container_weapon_ticker";
    }
}
