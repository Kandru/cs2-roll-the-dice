using CounterStrikeSharp.API.Modules.Utils;

namespace Conquest.Utils
{
    public static class Vectors
    {
        public static float GetDistance(Vector a, Vector b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            float dz = a.Z - b.Z;
            return MathF.Sqrt((dx * dx) + (dy * dy) + (dz * dz));
        }

        public static QAngle GetLookAtAngle(Vector source, Vector target)
        {
            // Calculate direction vector from source to target
            Vector direction = new(target.X - source.X, target.Y - source.Y, target.Z - source.Z);
            // Calculate yaw angle (horizontal rotation)
            float yaw = (float)(Math.Atan2(direction.Y, direction.X) * 180 / Math.PI);
            // Calculate pitch angle (vertical rotation)
            float distance = MathF.Sqrt((direction.X * direction.X) + (direction.Y * direction.Y));
            float pitch = (float)(-Math.Atan2(direction.Z, distance) * 180 / Math.PI);
            // Return QAngle with calculated pitch and yaw, roll is 0
            return new QAngle(pitch, yaw, 0);
        }

        public static Vector Normalize(Vector v)
        {
            float length = MathF.Sqrt((v.X * v.X) + (v.Y * v.Y) + (v.Z * v.Z));
            return length > 0 ? new Vector(v.X / length, v.Y / length, v.Z / length) : new Vector(0, 0, 0);
        }

        public static Vector Cross(Vector a, Vector b)
        {
            return new Vector(
                (a.Y * b.Z) - (a.Z * b.Y),
                (a.Z * b.X) - (a.X * b.Z),
                (a.X * b.Y) - (a.Y * b.X)
            );
        }

        public static Vector CalculatePositionInFront(Vector origin, QAngle eyeAngles, float offSetXY, float offSetZ = 0)
        {
            // Convert yaw angle from degrees to radians
            float yawAngleRadians = (float)(eyeAngles.Y * Math.PI / 180.0);

            // Calculate offsets in x and y directions
            float offsetX = offSetXY * (float)Math.Cos(yawAngleRadians);
            float offsetY = offSetXY * (float)Math.Sin(yawAngleRadians);

            // Calculate position in front of the player
            var positionInFront = new Vector
            {
                X = origin.X + offsetX,
                Y = origin.Y + offsetY,
                Z = origin.Z + offSetZ
            };

            return positionInFront;
        }
    }
}