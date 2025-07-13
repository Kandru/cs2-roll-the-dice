using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;

namespace Conquest.Utils
{
    public static class Players
    {
        public static void FreezePlayer(CCSPlayerController player)
        {
            if (player == null
                || !player.IsValid
                || player.Pawn?.Value == null)
            {
                return;
            }

            player.Pawn.Value.MoveType = MoveType_t.MOVETYPE_OBSOLETE;
            Utilities.SetStateChanged(player.Pawn.Value, "CBaseEntity", "m_MoveType");
            Schema.GetRef<MoveType_t>(player.Pawn.Value.Handle, "CBaseEntity", "m_nActualMoveType") = MoveType_t.MOVETYPE_OBSOLETE;
        }

        public static void UnfreezePlayer(CCSPlayerController player)
        {
            if (player == null
                || !player.IsValid
                || player.Pawn?.Value == null)
            {
                return;
            }

            player.Pawn.Value.MoveType = MoveType_t.MOVETYPE_WALK;
            Utilities.SetStateChanged(player.Pawn.Value, "CBaseEntity", "m_MoveType");
            Schema.GetRef<MoveType_t>(player.Pawn.Value.Handle, "CBaseEntity", "m_nActualMoveType") = MoveType_t.MOVETYPE_WALK;
        }
    }
}