using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace RollTheDice.Utils
{
    public static class RayTrace
    {
        private static readonly TraceOptions Options = new()
        {
            InteractsWith = Masks.PlayerSolid | Contents.PhysicsProp | Contents.WorldGeometry,
        };

        // world and props only: players never block (sight lines from one player to another). Map walls are Solid;
        // WorldGeometry alone does not stop at them.
        private static readonly TraceOptions WorldOptions = new()
        {
            InteractsWith = Contents.Solid | Contents.Window | Contents.PhysicsProp | Contents.WorldGeometry,
        };

        public static TraceResult TraceLine(Vector start, Vector end, CBaseEntity? ignore = null)
            => Trace.TraceEndShape(start, end, ignore, Options);

        /// <summary>
        /// Like <see cref="TraceLine"/> but players do not stop it. A player trace at a target ends on its hull a little
        /// before the target point, so "hit nothing" cannot be told from "hit the target" there.
        /// </summary>
        public static TraceResult TraceWorld(Vector start, Vector end, CBaseEntity? ignore = null)
            => Trace.TraceEndShape(start, end, ignore, WorldOptions);
    }
}
