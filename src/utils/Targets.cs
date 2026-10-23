using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;

namespace RollTheDice.Utils
{
    /// <summary>Living players around a point: area effects (smokes, flare, heartbeat) only touch the right team.</summary>
    public static class Targets
    {
        public const float EyeHeight = 64f;

        public static bool IsAlive(CCSPlayerController? player) =>
            player is { IsValid: true, PawnIsAlive: true } && player.PlayerPawn.Value is { IsValid: true };

        // free-for-all modes (deathmatch) make teammates enemies too
        private static readonly ConVar? TeammatesAreEnemies = ConVar.Find("mp_teammates_are_enemies");

        public static bool IsEnemy(CCSPlayerController a, CCSPlayerController b) =>
            a != b
            && a.Team is CsTeam.CounterTerrorist or CsTeam.Terrorist
            && b.Team is CsTeam.CounterTerrorist or CsTeam.Terrorist
            && (a.Team != b.Team || TeammatesAreEnemies?.GetPrimitiveValue<bool>() == true);

        /// <summary>Living players within <paramref name="radius"/> of <paramref name="center"/> (3D distance).</summary>
        public static IEnumerable<(CCSPlayerController Player, CCSPlayerPawn Pawn, float Distance)> Around(Vector center, float radius)
        {
            foreach (CCSPlayerController player in Utilities.GetPlayers())
            {
                if (!IsAlive(player) || player.PlayerPawn.Value is not { AbsOrigin: { } origin } pawn)
                {
                    continue;
                }
                float distance = Vectors.GetDistance(center, origin);
                if (distance <= radius)
                {
                    yield return (player, pawn, distance);
                }
            }
        }

        public static Vector Eyes(CCSPlayerPawn pawn)
        {
            Vector origin = pawn.AbsOrigin ?? new Vector(0, 0, 0);
            return new Vector(origin.X, origin.Y, origin.Z + EyeHeight);
        }

        /// <summary>True when nothing solid lies between the two pawns' eyes.</summary>
        public static bool CanSee(CCSPlayerPawn from, CCSPlayerPawn to)
        {
            TraceResult trace = RayTrace.TraceLine(Eyes(from), Eyes(to), from);
            return !trace.DidHit() || trace.Fraction > 0.97f || (trace.HitEntity() is { } hit && hit.Handle == to.Handle);
        }
    }
}
