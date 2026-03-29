using System;
using System.Numerics;

namespace IsometricMagic.Engine.Particles
{
    public sealed class ColorGradient
    {
        public struct Key
        {
            public float Time;
            public Vector4 Color;

            public Key(float time, Vector4 color)
            {
                Time = time;
                Color = color;
            }
        }

        private Key[] _keys;
        private Vector4[] _lut = Array.Empty<Vector4>();
        private bool _dirty = true;
        private int _resolution = 128;

        public int Resolution
        {
            get => _resolution;
            set
            {
                var next = value < 2 ? 2 : value;
                if (_resolution == next) return;
                _resolution = next;
                _dirty = true;
            }
        }

        public ColorGradient()
        {
            _keys = new[]
            {
                new Key(0f, new Vector4(1f, 1f, 1f, 1f)),
                new Key(1f, new Vector4(1f, 1f, 1f, 1f))
            };
        }

        public static ColorGradient Solid(Vector4 color)
        {
            var gradient = new ColorGradient();
            gradient.SetKeys(new Key(0f, color), new Key(1f, color));
            return gradient;
        }

        public void SetKeys(params Key[] keys)
        {
            if (keys == null || keys.Length == 0)
            {
                _keys = new[]
                {
                    new Key(0f, new Vector4(1f, 1f, 1f, 1f)),
                    new Key(1f, new Vector4(1f, 1f, 1f, 1f))
                };
                _dirty = true;
                return;
            }

            var sanitized = new Key[keys.Length];
            for (var i = 0; i < keys.Length; i++)
            {
                var time = Clamp01(keys[i].Time);
                sanitized[i] = new Key(time, keys[i].Color);
            }

            Array.Sort(sanitized, (a, b) => a.Time.CompareTo(b.Time));

            if (sanitized.Length == 1)
            {
                _keys = new[] { sanitized[0], new Key(1f, sanitized[0].Color) };
            }
            else
            {
                _keys = sanitized;
            }

            _dirty = true;
        }

        public Vector4 Evaluate(float t)
        {
            EnsureLut();

            if (_lut.Length == 0)
            {
                return new Vector4(1f, 1f, 1f, 1f);
            }

            var clamped = Clamp01(t);
            var index = (int) (clamped * (_lut.Length - 1));
            index = index < 0 ? 0 : index >= _lut.Length ? _lut.Length - 1 : index;
            return _lut[index];
        }

        public void Prepare()
        {
            EnsureLut();
        }

        private void EnsureLut()
        {
            if (!_dirty) return;
            _dirty = false;

            if (_keys.Length == 0)
            {
                _lut = Array.Empty<Vector4>();
                return;
            }

            _lut = new Vector4[_resolution];
            var lastIndex = _resolution - 1;
            var keyCount = _keys.Length;

            var keyIndex = 0;
            for (var i = 0; i < _resolution; i++)
            {
                var t = lastIndex == 0 ? 0f : i / (float) lastIndex;

                while (keyIndex < keyCount - 2 && t > _keys[keyIndex + 1].Time)
                {
                    keyIndex++;
                }

                var a = _keys[keyIndex];
                var b = _keys[Math.Min(keyIndex + 1, keyCount - 1)];

                var span = b.Time - a.Time;
                var localT = span > 0f ? (t - a.Time) / span : 0f;
                localT = Clamp01(localT);

                _lut[i] = Lerp(a.Color, b.Color, localT);
            }
        }

        private static Vector4 Lerp(Vector4 a, Vector4 b, float t)
        {
            return a + (b - a) * t;
        }

        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            return value > 1f ? 1f : value;
        }
    }
}
