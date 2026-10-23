using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Configs;
using RollTheDice.Utils;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    /// <summary>A UAV: every few seconds all enemies show on the radar of the holder's team for a moment.</summary>
    public class RadarScan : DiceBlueprint
    {
        public override string ClassName => "RadarScan";
        public override List<string> Listeners { get; } = ["OnTick"];
        private float _nextScanAt;
        private float _scanUntil;

        public RadarScan(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Reset()
        {
            base.Reset();
            _nextScanAt = 0f;
            _scanUntil = 0f;
        }

        /// <summary>
        /// The engine works out the spotted state every tick, so during a scan it is written every tick: the enemy
        /// pawn is spotted by every living teammate of a holder.
        /// </summary>
        public void OnTick()
        {
            if (_players.Count == 0)
            {
                return;
            }

            RadarScanConfig config = _config.Dices.RadarScan;
            float now = Server.CurrentTime;
            if (now >= _nextScanAt)
            {
                _nextScanAt = now + config.IntervalSeconds;
                _scanUntil = now + config.ScanSeconds;
                foreach (CCSPlayerController holder in _players)
                {
                    Alert(holder, "dice_RadarScan_scan");
                }
            }
            if (now >= _scanUntil)
            {
                return;
            }

            List<CCSPlayerController> players = Utilities.GetPlayers();
            foreach (CCSPlayerController holder in _players)
            {
                if (!holder.IsValid)
                {
                    continue;
                }
                uint low = 0;
                uint high = 0;
                foreach (CCSPlayerController mate in players)
                {
                    if (mate.IsValid && !Targets.IsEnemy(holder, mate) && mate.Team == holder.Team && mate.Slot is >= 0 and < 64)
                    {
                        if (mate.Slot < 32)
                        {
                            low |= 1u << mate.Slot;
                        }
                        else
                        {
                            high |= 1u << (mate.Slot - 32);
                        }
                    }
                }
                foreach (CCSPlayerController enemy in players)
                {
                    if (!Targets.IsEnemy(holder, enemy) || !Targets.IsAlive(enemy) || enemy.PlayerPawn.Value is not { IsValid: true } pawn)
                    {
                        continue;
                    }
                    EntitySpottedState_t spotted = pawn.EntitySpottedState;
                    spotted.Spotted = true;
                    spotted.SpottedByMask[0] |= low;
                    spotted.SpottedByMask[1] |= high;
                    Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_entitySpottedState");
                }
            }
        }
    }
}

namespace RollTheDice.Configs
{
    public class RadarScanConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("interval_seconds")] public float IntervalSeconds { get; set; } = 15f;
        [JsonPropertyName("scan_seconds")] public float ScanSeconds { get; set; } = 3f;
    }
}
