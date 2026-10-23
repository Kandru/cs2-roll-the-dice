using CounterStrikeSharp.API.Core;

namespace RollTheDice.Utils
{
    public static class Grenades
    {
        /// <summary>
        /// The player who threw a grenade projectile. The thrower is set after the spawn: read it one frame after
        /// OnEntitySpawned (Ender Pearl, homing grenades, proximity mines).
        /// </summary>
        public static CCSPlayerController? ThrowerOf(CBaseGrenade grenade)
        {
            if (!grenade.IsValid)
            {
                return null;
            }
            CCSPlayerPawn? pawn = grenade.Thrower.Value;
            return pawn?.Controller.Value?.As<CCSPlayerController>() is { IsValid: true } player ? player : null;
        }
    }
}
