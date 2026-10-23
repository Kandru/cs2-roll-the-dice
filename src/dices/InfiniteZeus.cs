using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Configs;
using RollTheDice.Utils;
using System.Drawing;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    /// <summary>Zeus railgun: the taser reaches across the map and leaves a visible bolt.</summary>
    public class InfiniteZeus : DiceBlueprint
    {
        public override string ClassName => "InfiniteZeus";
        public override List<string> Events { get; } = ["EventWeaponFire"];
        // the engine's own taser range: hits closer than this are left to the game
        private const float TaserRange = 190f;
        private const float MaxRange = 8192f;
        private static readonly TraceOptions ShotOptions = new() { InteractsWith = Masks.Shot };

        public InfiniteZeus(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            base.Add(player);
            if (_players.Contains(player))
            {
                Weapons.GiveOnce(player, "weapon_taser");
            }
        }

        /// <summary>A Zeus shot of the holder: a trace along the view; an enemy beyond the normal range dies.</summary>
        public HookResult EventWeaponFire(EventWeaponFire @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (_players.Count == 0
                || player is not { IsValid: true }
                || !_players.Contains(player)
                || @event.Weapon != "weapon_taser"
                || player.PlayerPawn.Value is not { IsValid: true } pawn)
            {
                return HookResult.Continue;
            }

            Vector eyes = Targets.Eyes(pawn);
            Vector forward = Entities.GetForwardVector(pawn.EyeAngles);
            Vector end = new(eyes.X + (forward.X * MaxRange), eyes.Y + (forward.Y * MaxRange), eyes.Z + (forward.Z * MaxRange));
            TraceResult trace = Trace.TraceEndShape(eyes, end, pawn, ShotOptions);
            Vector hit = trace.DidHit() ? trace.EndPos : end;
            Bolt(eyes, hit, _config.Dices.InfiniteZeus.BoltSeconds);

            float distance = Vectors.GetDistance(eyes, hit);
            if (distance <= TaserRange
                || trace.HitEntity() is not { IsValid: true, DesignerName: "player" } entity
                || entity.As<CCSPlayerPawn>() is not { IsValid: true } victimPawn
                || victimPawn.Controller.Value?.As<CCSPlayerController>() is not { IsValid: true } victim
                || !Targets.IsEnemy(player, victim))
            {
                return HookResult.Continue;
            }

            Debug($"{player.PlayerName} railgun hit {victim.PlayerName} at {distance:0}");
            Damage.Deal(victimPawn, pawn, _config.Dices.InfiniteZeus.Damage, DamageTypes_t.DMG_SHOCK);
            return HookResult.Continue;
        }

        /// <summary>A short-lived beam from the muzzle to the hit (entity I/O removes it, no timer).</summary>
        private static void Bolt(Vector from, Vector to, float seconds)
        {
            if (Utilities.CreateEntityByName<CBeam>("beam") is not { } beam)
            {
                return;
            }
            beam.Render = Color.FromArgb(255, 120, 200, 255);
            beam.Width = 3f;
            beam.Teleport(from, new QAngle(0, 0, 0), new Vector(0, 0, 0));
            beam.EndPos.X = to.X;
            beam.EndPos.Y = to.Y;
            beam.EndPos.Z = to.Z;
            beam.DispatchSpawn();
            beam.AddEntityIOEvent("Kill", null, null, "", seconds);
        }
    }
}

namespace RollTheDice.Configs
{
    public class InfiniteZeusConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        // damage of a long-range hit (500 kills through any armor)
        [JsonPropertyName("damage")] public float Damage { get; set; } = 500f;
        [JsonPropertyName("bolt_seconds")] public float BoltSeconds { get; set; } = 0.3f;
    }
}
