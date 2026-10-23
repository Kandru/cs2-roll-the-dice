using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;

namespace RollTheDice.Utils
{
    /// <summary>Beams between two points (Zeus bolts, chain lightning, laser pointer).</summary>
    public static class Beam
    {
        public static readonly Color Lightning = Color.FromArgb(255, 120, 200, 255);

        /// <summary>A beam that removes itself after <paramref name="seconds"/> (entity I/O, no timer).</summary>
        public static void Flash(Vector from, Vector to, float seconds, Color color, float width = 3f)
        {
            if (Create(from, to, color, width) is { } beam)
            {
                beam.AddEntityIOEvent("Kill", null, null, "", seconds);
            }
        }

        public static CBeam? Create(Vector from, Vector to, Color color, float width)
        {
            if (Utilities.CreateEntityByName<CBeam>("beam") is not { } beam)
            {
                return null;
            }
            beam.Render = color;
            beam.Width = width;
            beam.Teleport(from, new QAngle(0, 0, 0), new Vector(0, 0, 0));
            beam.EndPos.X = to.X;
            beam.EndPos.Y = to.Y;
            beam.EndPos.Z = to.Z;
            beam.DispatchSpawn();
            return beam;
        }

        /// <summary>Moves both ends of a living beam (laser pointer).</summary>
        public static void Move(CBeam beam, Vector from, Vector to)
        {
            if (!beam.IsValid)
            {
                return;
            }
            beam.Teleport(from, null, null);
            beam.EndPos.X = to.X;
            beam.EndPos.Y = to.Y;
            beam.EndPos.Z = to.Z;
            Utilities.SetStateChanged(beam, "CBeam", "m_vecEndPos");
        }
    }
}
