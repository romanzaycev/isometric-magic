using System.Numerics;

namespace IsometricMagic.Engine.Graphics.Lighting
{
    public class Light2D
    {
        public Vector2 Position;
        public Vector3 Color = new(1f, 1f, 1f);
        public float Intensity = 1f;
        public float Radius = 1024f;
        public float Height = 1.5f;
        public float Falloff = 2f;
        public float InnerRadius = 64f;
        public float CenterAttenuation = 0.75f;

        public Light2D(Vector2 position)
        {
            Position = position;
        }
    }
}
