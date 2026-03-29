using System;

namespace IsometricMagic.Engine.Tweening
{
    public delegate float EaseFunc(float t);

    public static class Easing
    {
        public static float Linear(float t) => t;

        public static float InQuad(float t) => t * t;

        public static float OutQuad(float t)
        {
            var inv = 1f - t;
            return 1f - inv * inv;
        }

        public static float InOutQuad(float t)
        {
            return t < 0.5f
                ? 2f * t * t
                : 1f - MathF.Pow(-2f * t + 2f, 2f) / 2f;
        }

        public static float InCubic(float t) => t * t * t;

        public static float OutCubic(float t)
        {
            var inv = 1f - t;
            return 1f - inv * inv * inv;
        }

        public static float InOutCubic(float t)
        {
            return t < 0.5f
                ? 4f * t * t * t
                : 1f - MathF.Pow(-2f * t + 2f, 3f) / 2f;
        }
    }
}
