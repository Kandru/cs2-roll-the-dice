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
    public class SolarFlare : DiceBlueprint
    {
        public override string ClassName => "SolarFlare";
        public override List<string> Listeners { get; } = ["OnPlayerButtonsChanged"];
        private readonly Dictionary<CCSPlayerController, int> _usesLeft = [];

        public SolarFlare(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            base.Add(player);
            if (_players.Contains(player))
            {
                _usesLeft[player] = _config.Dices.SolarFlare.Uses;
            }
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            base.Remove(player, reason);
            _ = _usesLeft.Remove(player);
        }

        public override void Reset()
        {
            base.Reset();
            _usesLeft.Clear();
        }

        /// <summary>USE (not while defusing): blinds every enemy in range that looks at the holder and can see them.</summary>
        public void OnPlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
        {
            if (_usesLeft.Count == 0
                || (pressed & PlayerButtons.Use) == 0
                || !_usesLeft.TryGetValue(player, out int left)
                || left <= 0
                || !IsAlivePlayer(player)
                || player.PlayerPawn.Value is not { IsValid: true } pawn
                || pawn.IsDefusing)
            {
                return;
            }

            SolarFlareConfig config = _config.Dices.SolarFlare;
            Vector eyes = Targets.Eyes(pawn);
            int blinded = 0;
            foreach ((CCSPlayerController enemy, CCSPlayerPawn enemyPawn, float distance) in Targets.Around(eyes, config.Range))
            {
                if (!Targets.IsEnemy(player, enemy) || distance < 1f || !LooksAt(enemyPawn, eyes, distance, config.ViewCone) || !Targets.CanSee(enemyPawn, pawn))
                {
                    continue;
                }
                FlashRounds.Blind(enemyPawn, config.BlindSeconds);
                Alert(enemy, "dice_SolarFlare_victim", new() { { "playerName", player.PlayerName } });
                blinded++;
            }

            _usesLeft[player] = left - 1;
            Alert(player, "dice_SolarFlare_used", new() { { "count", blinded.ToString() } });
        }

        /// <summary>True when <paramref name="point"/> lies within the viewer's cone (dot product of view and direction).</summary>
        private static bool LooksAt(CCSPlayerPawn viewer, Vector point, float distance, float minDot)
        {
            Vector from = Targets.Eyes(viewer);
            Vector forward = Entities.GetForwardVector(viewer.EyeAngles);
            float dot = ((point.X - from.X) * forward.X + (point.Y - from.Y) * forward.Y + (point.Z - from.Z) * forward.Z) / distance;
            return dot >= minDot;
        }
    }
}

namespace RollTheDice.Configs
{
    public class SolarFlareConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("uses")] public int Uses { get; set; } = 1;
        [JsonPropertyName("range")] public float Range { get; set; } = 600f;
        // how directly an enemy must look at the holder (1.0 = exactly, 0.7 = about 45 degrees off)
        [JsonPropertyName("view_cone")] public float ViewCone { get; set; } = 0.7f;
        [JsonPropertyName("blind_seconds")] public float BlindSeconds { get; set; } = 3f;
    }
}
