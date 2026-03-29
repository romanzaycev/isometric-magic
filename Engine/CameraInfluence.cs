using System.Drawing;
using System.Numerics;

namespace IsometricMagic.Engine
{
    public enum CameraInfluenceKind
    {
        SetCenter,
        AddOffset,
        ClampBounds,
        Shake
    }

    public readonly struct CameraInfluence
    {
        public CameraInfluenceKind Kind { get; }
        public int Priority { get; }
        public Vector2 Value { get; }
        public Rectangle? Bounds { get; }

        private CameraInfluence(CameraInfluenceKind kind, int priority, Vector2 value, Rectangle? bounds)
        {
            Kind = kind;
            Priority = priority;
            Value = value;
            Bounds = bounds;
        }

        public static CameraInfluence SetCenter(Vector2 center, int priority)
        {
            return new CameraInfluence(CameraInfluenceKind.SetCenter, priority, center, null);
        }

        public static CameraInfluence AddOffset(Vector2 offset, int priority)
        {
            return new CameraInfluence(CameraInfluenceKind.AddOffset, priority, offset, null);
        }

        public static CameraInfluence Shake(Vector2 offset, int priority)
        {
            return new CameraInfluence(CameraInfluenceKind.Shake, priority, offset, null);
        }

        public static CameraInfluence ClampBounds(Rectangle bounds, int priority)
        {
            return new CameraInfluence(CameraInfluenceKind.ClampBounds, priority, Vector2.Zero, bounds);
        }
    }
}
