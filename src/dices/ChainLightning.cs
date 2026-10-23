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

            ChainLightningConfig config = _config.Dices.ChainLightning;
            CCSPlayerPawn? attacker = holder.PlayerPawn.Value is { IsValid: true } p ? p : null;
            HashSet<CCSPlayerController> struck = [first];
            CCSPlayerPawn from = firstPawn;
            for (int jump = 0; jump < config.Jumps; jump++)
            {
                (CCSPlayerController Player, CCSPlayerPawn Pawn)? next = Nearest(holder, from, struck, config.Range);
                if (next is not { } target)
                {
                    break;
                }
                _ = struck.Add(target.Player);
                Beam.Flash(Body(from), Body(target.Pawn), config.BoltSeconds, Beam.Lightning, BoltWidth);
                int before = target.Pawn.Health;
                Damage.Deal(target.Pawn, attacker, config.Damage, DamageTypes_t.DMG_SHOCK);
                Debug($"{holder.PlayerName} chain lightning jumps to {target.Player.PlayerName} ({before} -> {target.Pawn.Health} HP)");
                from = target.Pawn;
            }
            return HookResult.Continue;
        }

        private static (CCSPlayerController, CCSPlayerPawn)? Nearest(CCSPlayerController holder, CCSPlayerPawn from, HashSet<CCSPlayerController> struck, float range)
        {
            (CCSPlayerController, CCSPlayerPawn)? best = null;
            float bestDistance = float.MaxValue;
            foreach ((CCSPlayerController player, CCSPlayerPawn pawn, float distance) in Targets.Around(Body(from), range))
            {
                if (distance < bestDistance && !struck.Contains(player) && Targets.IsEnemy(holder, player) && Targets.CanSee(from, pawn))
                {
                    best = (player, pawn);
                    bestDistance = distance;
                }
            }
            return best;
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
        [JsonPropertyName("range")] public float Range { get; set; } = 400f;
        [JsonPropertyName("damage")] public float Damage { get; set; } = 50f;
        [JsonPropertyName("bolt_seconds")] public float BoltSeconds { get; set; } = 0.8f;
    }
}
