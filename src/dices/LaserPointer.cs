using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Configs;
using RollTheDice.Utils;
using System.Drawing;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    /// <summary>A laser in team colour shows where the holder aims (and gives them away).</summary>
    public class LaserPointer : DiceBlueprint
    {
        public override string ClassName => "LaserPointer";
        public override List<string> Listeners { get; } = ["OnTick"];
        private const float MaxRange = 4096f;
        // the beam leaves a bit ahead, below and right of the eyes, like a laser on the gun; starting right at the eyes
        // it filled the holder's own view
        private const float AheadOffset = 40f;
        private const float DownOffset = 6f;
        private const float RightOffset = 5f;
        private static readonly Color TerroristColor = Color.FromArgb(255, 255, 80, 40);
        private static readonly Color CounterTerroristColor = Color.FromArgb(255, 60, 140, 255);

        private readonly Dictionary<CCSPlayerController, CBeam> _beams = [];

        public LaserPointer(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            base.Remove(player, reason);
            if (_beams.Remove(player, out CBeam? beam))
            {
                Entities.RemoveEntity(beam);
            }
        }

        public override void Reset()
        {
            base.Reset();
            foreach (CBeam beam in _beams.Values)
            {
                Entities.RemoveEntity(beam);
            }
            _beams.Clear();
        }

        /// <summary>Every second tick the beam follows the view to the first thing it hits.</summary>
        public void OnTick()
        {
            if (_players.Count == 0 || Server.TickCount % 2 != 0)
            {
                return;
            }

            foreach (CCSPlayerController player in _players)
            {
                if (!IsAlivePlayer(player) || player.PlayerPawn.Value is not { IsValid: true } pawn)
                {
                    continue;
                }

                Vector eyes = Targets.Eyes(pawn);
                Vector forward = Entities.GetForwardVector(pawn.EyeAngles);
                Vector right = Entities.GetRightVector(pawn.EyeAngles);
                Vector start = new(
                    eyes.X + (forward.X * AheadOffset) + (right.X * RightOffset),
                    eyes.Y + (forward.Y * AheadOffset) + (right.Y * RightOffset),
                    eyes.Z + (forward.Z * AheadOffset) - DownOffset);
                Vector end = new(eyes.X + (forward.X * MaxRange), eyes.Y + (forward.Y * MaxRange), eyes.Z + (forward.Z * MaxRange));
                TraceResult trace = RayTrace.TraceLine(eyes, end, pawn);
                Vector hit = trace.DidHit() ? trace.EndPos : end;

                if (_beams.TryGetValue(player, out CBeam? beam) && beam.IsValid)
                {
                    Beam.Move(beam, start, hit);
                }
                else if (Beam.Create(start, hit, player.Team == CsTeam.Terrorist ? TerroristColor : CounterTerroristColor, _config.Dices.LaserPointer.Width) is { } created)
                {
                    _beams[player] = created;
                }
            }
        }
    }
}

namespace RollTheDice.Configs
{
    public class LaserPointerConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("width")] public float Width { get; set; } = 0.5f;
    }
}
