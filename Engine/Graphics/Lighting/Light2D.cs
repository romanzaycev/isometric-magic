using System.Numerics;

namespace IsometricMagic.Engine.Graphics.Lighting
{
    public class Light2D
    {
        public Vector2 Position;
        public Vector3 Color = new(1f, 1f, 1f);
        public float Intensity = 1f;

        public Light2D(Vector2 position)
        {
            Position = position;
        }
    }
}
