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

        public static TraceResult TraceLine(Vector start, Vector end, CBaseEntity? ignore = null)
            => Trace.TraceEndShape(start, end, ignore, Options);
    }
}
