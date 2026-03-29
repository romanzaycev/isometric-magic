using System.Numerics;

namespace IsometricMagic.Engine
{
    public enum OutlineLayering
    {
        Under,
        Over
    }

    public sealed class SpriteOutline
    {
        public bool Enabled;
        public float ThicknessTexels = 1f;
        public Vector4 Color = new(1f, 1f, 1f, 1f);
        public OutlineLayering Layering = OutlineLayering.Under;
    }
}
