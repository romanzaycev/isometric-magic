using System.Numerics;

namespace IsometricMagic.Engine.Tweening
{
    public static class Interp
    {
        public static float Clamp01(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            return t;
        }

        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        public static double Lerp(double a, double b, float t)
        {
            return a + (b - a) * t;
        }

        public static Vector2 Lerp(Vector2 a, Vector2 b, float t)
        {
            return a + (b - a) * t;
        }

        public static Vector3 Lerp(Vector3 a, Vector3 b, float t)
        {
            return a + (b - a) * t;
        }
    }
}
