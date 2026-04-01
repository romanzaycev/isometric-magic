using System.Numerics;

namespace IsometricMagic.Game.Model
{
    public readonly record struct CanvasPosition(float X, float Y)
    {
        public Vector2 ToVector2()
        {
            return new Vector2(X, Y);
        }

        public static CanvasPosition FromVector2(Vector2 value)
        {
            return new CanvasPosition(value.X, value.Y);
        }

        public static CanvasPosition operator +(CanvasPosition position, Vector2 delta)
        {
            return new CanvasPosition(position.X + delta.X, position.Y + delta.Y);
        }

        public static Vector2 operator -(CanvasPosition left, CanvasPosition right)
        {
            return new Vector2(left.X - right.X, left.Y - right.Y);
        }
    }
}
