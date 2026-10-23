using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using System.Drawing;

namespace RollTheDice.Utils
{
    /// <summary>Colours a player model (burning, frozen) and gives it its normal look back.</summary>
    public static class Tint
    {
        public static readonly Color Normal = Color.FromArgb(255, 255, 255, 255);

        public static void Set(CCSPlayerPawn pawn, Color color)
        {
            if (!pawn.IsValid || pawn.Render == color)
            {
                return;
            }
            pawn.Render = color;
            Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
        }

        public static void Reset(CCSPlayerPawn? pawn)
        {
            if (pawn is { IsValid: true })
            {
                Set(pawn, Normal);
            }
        }
    }
}
