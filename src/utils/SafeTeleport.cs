using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace RollTheDice.Utils
{
    /// <summary>
    /// Teleport that does not leave a player stuck: the target is moved off the surface it was found on, away from
    /// close walls and below a low ceiling, and refused when another player stands there, a player does not fit or
    /// cannot walk there (player clip, off the nav mesh).
    /// </summary>
    public static class SafeTeleport
    {
        private const float HullHalfWidth = 16f;
        private const float HullHeight = 72f;
        private const float MinHeadroom = 56f;
        private const float SurfaceOffset = 20f;
        private const float PlayerClearance = 48f;
        // nav areas keep a border to walls; a spot farther than this from any area is off the playable map
        private const float MaxNavDistance = 64f;
        // deepest drop below a pearl's hit that still counts as a floor
        private const float MaxFloorDrop = 1024f;
        private static readonly TraceOptions ClipOptions = new() { InteractsWith = Masks.PlayerSolidBrushOnly };

        /// <summary>Why the last <see cref="TryTeleport"/> refused (debug output).</summary>
        public static string LastRefusal { get; private set; } = string.Empty;

        public static bool TryTeleport(CCSPlayerPawn pawn, Vector target, Vector? surfaceNormal = null)
        {
            if (!pawn.IsValid)
            {
                return false;
            }

            float x = target.X;
            float y = target.Y;
            float z = target.Z + 4f;
            if (surfaceNormal is { } n && !n.IsZero())
            {
                x += n.X * SurfaceOffset;
                y += n.Y * SurfaceOffset;
                z += Math.Max(0f, n.Z) * 4f;
            }

            // a low ceiling: move down so the head fits, refuse a crawl space
            float headroom = Room(pawn, x, y, z, 0, 0, 1, HullHeight);
            if (headroom < HullHeight)
            {
                float drop = HullHeight - headroom;
                float floor = Room(pawn, x, y, z, 0, 0, -1, drop);
                z -= Math.Min(drop, floor);
                if (Room(pawn, x, y, z, 0, 0, 1, HullHeight) < MinHeadroom)
                {
                    LastRefusal = "crawl space";
                    return false;
                }
            }

            // back off walls at waist height
            float waist = z + (HullHeight / 2f);
            // a wall closer on one side moves the spot towards the other (room +x = 5, -x = 16 -> 11 units to -x)
            x += Room(pawn, x, y, waist, 1, 0, 0, HullHalfWidth) - Room(pawn, x, y, waist, -1, 0, 0, HullHalfWidth);
            y += Room(pawn, x, y, waist, 0, 1, 0, HullHalfWidth) - Room(pawn, x, y, waist, 0, -1, 0, HullHalfWidth);

            Vector spot = new(x, y, z);
            if (!IsReachable(pawn, spot))
            {
                return false;
            }
            foreach ((CCSPlayerController _, CCSPlayerPawn other, float _) in Targets.Around(spot, PlayerClearance))
            {
                if (other.Handle != pawn.Handle)
                {
                    LastRefusal = "player there";
                    return false;
                }
            }

            pawn.Teleport(spot, null, new Vector(0, 0, 0));
            return true;
        }

        /// <summary>
        /// False for spots a player cannot walk to: inside a player clip (grenades fly through those, so a throw
        /// reaches roofs and the outside of the map) or above ground away from the nav mesh, which only covers the
        /// playable area. The nav check uses the floor below the spot, so a hit high on a wall counts where the player
        /// lands. A map without a nav mesh skips it.
        /// </summary>
        private static bool IsReachable(CCSPlayerPawn pawn, Vector spot)
        {
            TraceResult hull = Trace.TraceHullShape(
                spot,
                new Vector(spot.X, spot.Y, spot.Z + 1f),
                new Vector(-HullHalfWidth, -HullHalfWidth, 2f),
                new Vector(HullHalfWidth, HullHalfWidth, MinHeadroom),
                pawn,
                ClipOptions);
            if (hull.IsAllSolid)
            {
                LastRefusal = "inside a player clip";
                return false;
            }
            if (CCSNavArea.GetAllNavAreas().Count == 0)
            {
                return true;
            }

            // no floor below (a throw off the map edge) is never reachable
            TraceResult floor = Trace.TraceEndShape(spot, new Vector(spot.X, spot.Y, spot.Z - MaxFloorDrop), pawn, ClipOptions);
            if (!floor.DidHit())
            {
                LastRefusal = "no floor below";
                return false;
            }
            if (CCSNavArea.GetClosestNavArea(floor.EndPos, MaxNavDistance) == null)
            {
                LastRefusal = $"floor off the nav mesh at {floor.EndPos}";
                return false;
            }
            return true;
        }

        /// <summary>Free distance from the point along the direction, at most <paramref name="max"/>.</summary>
        private static float Room(CCSPlayerPawn pawn, float x, float y, float z, float dx, float dy, float dz, float max)
        {
            TraceResult trace = RayTrace.TraceLine(new Vector(x, y, z), new Vector(x + (dx * max), y + (dy * max), z + (dz * max)), pawn);
            return trace.DidHit() ? trace.Fraction * max : max;
        }
    }
}
