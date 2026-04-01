using System.Numerics;

namespace IsometricMagic.Game.Model
{
    public readonly record struct IsoWorldPosition(float X, float Y)
    {
        public Vector2 ToVector2()
        {
            return new Vector2(X, Y);
        }

        public static IsoWorldPosition FromVector2(Vector2 value)
        {
            return new IsoWorldPosition(value.X, value.Y);
        }

        public static IsoWorldPosition operator +(IsoWorldPosition position, Vector2 delta)
        {
            return new IsoWorldPosition(position.X + delta.X, position.Y + delta.Y);
        }

        public static Vector2 operator -(IsoWorldPosition left, IsoWorldPosition right)
        {
            return new Vector2(left.X - right.X, left.Y - right.Y);
        }
    }
}
