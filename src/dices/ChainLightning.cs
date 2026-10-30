using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Configs;
using RollTheDice.Utils;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    public class ChainLightning : DiceBlueprint
    {
        public override string ClassName => "ChainLightning";
        public override List<string> Events { get; } = ["EventPlayerHurt"];
        // thicker than the Zeus bolt: the jump happens away from the shooter and must be seen from afar
        private const float BoltWidth = 6f;

        public ChainLightning(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
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

        /// <summary>
        /// A Zeus hit of the holder jumps on to the next enemies in sight. The jumps are dice damage without the taser
        /// weapon name, so they never start another chain.
        /// </summary>
        public HookResult EventPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
        {
            CCSPlayerController? holder = @event.Attacker;
            CCSPlayerController? first = @event.Userid;
            if (_players.Count == 0
                || Damage.IsDealing
                || holder is not { IsValid: true }
                || !_players.Contains(holder)
                || !@event.Weapon.Contains("taser", StringComparison.OrdinalIgnoreCase)
                || first is not { IsValid: true }
                || first.PlayerPawn.Value is not { IsValid: true } firstPawn)
            {
                return HookResult.Continue;
            }

            // player_hurt fires inside the Zeus hit's TakeDamage: chain damage dealt right here would nest (see Thorns)
            Vector start = Body(firstPawn);
            Debug($"{holder.PlayerName} Zeus hit {first.PlayerName}, chain starts");
            Server.NextFrame(() => Chain(holder, first, start));
            return HookResult.Continue;
        }

        /// <summary>The first jump starts where the Zeus victim stood when hit (it is usually dead by now).</summary>
        private void Chain(CCSPlayerController holder, CCSPlayerController first, Vector start)
        {
            if (!holder.IsValid)
            {
                return;
            }

            ChainLightningConfig config = _config.Dices.ChainLightning;
            float reach = config.Reach;
            CCSPlayerPawn? attacker = holder.PlayerPawn.Value is { IsValid: true } p ? p : null;
            HashSet<CCSPlayerController> struck = [first];
            for (int jump = 0; jump < config.Jumps; jump++)
            {
                (CCSPlayerController Player, CCSPlayerPawn Pawn, float Distance)? next = Nearest(holder, start, struck, reach);
                if (next is not { } target)
                {
                    if (jump == 0)
                    {
                        Debug($"{holder.PlayerName} chain lightning finds no enemy in sight within {reach:0}: {DescribeNearest(holder, start, struck)}");
                    }
                    break;
                }
                _ = struck.Add(target.Player);
                Vector end = Body(target.Pawn);
                Beam.Flash(start, end, config.BoltSeconds, Beam.Lightning, BoltWidth);
                int before = target.Pawn.Health;
                float percent = config.DamagePercentAt(target.Distance);
                // a share of the larger of max and current health: 100 % is lethal even above max health
                float amount = Math.Max(target.Pawn.MaxHealth, target.Pawn.Health) * percent / 100f;
                Damage.Deal(target.Pawn, attacker, amount, DamageTypes_t.DMG_SHOCK);
                Debug($"{holder.PlayerName} chain lightning jumps {target.Distance:0} to {target.Player.PlayerName}, {percent:0} % ({before} -> {target.Pawn.Health} HP)");
                start = end;
            }
        }

        private static (CCSPlayerController, CCSPlayerPawn, float)? Nearest(CCSPlayerController holder, Vector start, HashSet<CCSPlayerController> struck, float range)
        {
            (CCSPlayerController, CCSPlayerPawn, float)? best = null;
            float bestDistance = float.MaxValue;
            foreach ((CCSPlayerController player, CCSPlayerPawn pawn, float distance) in Targets.Around(start, range))
            {
                if (distance < bestDistance && !struck.Contains(player) && Targets.IsEnemy(holder, player) && CanSee(start, pawn))
                {
                    best = (player, pawn, distance);
                    bestDistance = distance;
                }
            }
            return best;
        }

        /// <summary>
        /// No wall or prop between the jump's start and the target's body. Players do not block: a trace that stops at
        /// players ends on the target's hull short of its body, and the lightning may arc past others anyway.
        /// </summary>
        private static bool CanSee(Vector start, CCSPlayerPawn to)
        {
            TraceResult trace = RayTrace.TraceWorld(start, Body(to), to);
            return !trace.DidHit() || trace.Fraction > 0.98f;
        }

        /// <summary>Debug: the nearest living enemy at any range and why it was not taken.</summary>
        private static string DescribeNearest(CCSPlayerController holder, Vector start, HashSet<CCSPlayerController> struck)
        {
            (string Name, float Distance, CCSPlayerPawn Pawn)? nearest = null;
            foreach ((CCSPlayerController player, CCSPlayerPawn pawn, float distance) in Targets.Around(start, float.MaxValue))
            {
                if (!struck.Contains(player) && Targets.IsEnemy(holder, player) && (nearest is not { } n || distance < n.Distance))
                {
                    nearest = (player.PlayerName, distance, pawn);
                }
            }
            if (nearest is not { } found)
            {
                return "no other living enemy";
            }
            if (CanSee(start, found.Pawn))
            {
                return $"nearest enemy {found.Name} at {found.Distance:0}, in sight";
            }
            TraceResult trace = RayTrace.TraceWorld(start, Body(found.Pawn), found.Pawn);
            string blocker = trace.HitEntity()?.DesignerName ?? "?";
            return $"nearest enemy {found.Name} at {found.Distance:0}, blocked by {blocker} (contents {trace.Contents}) "
                + $"at {trace.Fraction:0.00} of the way{(trace.IsAllSolid ? ", starts inside it" : "")}";
        }

        private static Vector Body(CCSPlayerPawn pawn)
        {
            Vector origin = pawn.AbsOrigin ?? new Vector(0, 0, 0);
            return new Vector(origin.X, origin.Y, origin.Z + 40f);
        }
    }
}

namespace RollTheDice.Configs
{
    public class ChainLightningConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        // further enemies a Zeus hit jumps to
        [JsonPropertyName("jumps")] public int Jumps { get; set; } = 2;
        // damage by distance from the player the bolt jumps from (1 unit ≈ 2.5 cm): up to "range" units it deals
        // "damage_percent" of the target's max health (100 = lethal). The widest band is the jump's reach.
        // Default: 15 m lethal, 20 m 75 %, 25 m 50 %
        [JsonPropertyName("damage_bands")]
        public List<ChainLightningBand> DamageBands { get; set; } =
        [
            new() { Range = 600f, DamagePercent = 100f },
            new() { Range = 800f, DamagePercent = 75f },
            new() { Range = 1000f, DamagePercent = 50f },
        ];
        [JsonPropertyName("bolt_seconds")] public float BoltSeconds { get; set; } = 0.8f;

        [JsonIgnore] public float Reach => DamageBands.Count == 0 ? 0f : DamageBands.Max(static b => b.Range);

        /// <summary>The percent of the nearest band that reaches <paramref name="distance"/>; 0 beyond all bands.</summary>
        public float DamagePercentAt(float distance)
        {
            ChainLightningBand? band = DamageBands
                .Where(b => distance <= b.Range)
                .MinBy(static b => b.Range);
            return band?.DamagePercent ?? 0f;
        }
    }

    public class ChainLightningBand
    {
        [JsonPropertyName("range")] public float Range { get; set; }
        [JsonPropertyName("damage_percent")] public float DamagePercent { get; set; }
    }
}
