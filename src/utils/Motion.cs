using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace RollTheDice.Utils
{
    /// <summary>Velocity and ground state of a pawn (jumps, gliding, bouncing).</summary>
    public static class Motion
    {
        /// <summary>Units per second of a normal CS2 jump.</summary>
        public const float JumpVelocity = 300f;

        public static bool IsOnGround(CBasePlayerPawn pawn) => (pawn.Flags & (uint)PlayerFlags.FL_ONGROUND) != 0;

        public static Vector Velocity(CBasePlayerPawn pawn) =>
            pawn.AbsVelocity is { } v ? new Vector(v.X, v.Y, v.Z) : new Vector(0, 0, 0);

        public static float HorizontalSpeed(CBasePlayerPawn pawn) =>
            pawn.AbsVelocity is { } v ? MathF.Sqrt((v.X * v.X) + (v.Y * v.Y)) : 0f;

        /// <summary>Sets the velocity through Teleport, so the movement code takes it over.</summary>
        public static void SetVelocity(CBasePlayerPawn pawn, Vector velocity) => pawn.Teleport(null, null, velocity);

        public static void SetVerticalVelocity(CBasePlayerPawn pawn, float z)
        {
            Vector v = Velocity(pawn);
            v.Z = z;
            SetVelocity(pawn, v);
        }
    }

    /// <summary>
    /// Detects the tick a player lands (OnTick, one entry per watched player): reports the vertical speed of the
    /// last airborne tick, because on the landing tick the engine has already zeroed it.
    /// </summary>
    public sealed class LandingTracker
    {
        private readonly Dictionary<CCSPlayerController, float> _airborneZ = [];

        /// <summary>True on the landing tick; <paramref name="impactSpeed"/> is the falling speed (positive).</summary>
        public bool Landed(CCSPlayerController player, CBasePlayerPawn pawn, out float impactSpeed)
        {
            impactSpeed = 0f;
            if (!Motion.IsOnGround(pawn))
            {
                _airborneZ[player] = pawn.AbsVelocity?.Z ?? 0f;
                return false;
            }
            if (!_airborneZ.Remove(player, out float z))
            {
                return false;
            }
            impactSpeed = -z;
            return true;
        }

        public void Forget(CCSPlayerController player) => _airborneZ.Remove(player);

        public void Clear() => _airborneZ.Clear();
    }
}
