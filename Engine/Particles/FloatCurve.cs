using System;

namespace IsometricMagic.Engine.Particles
{
    public sealed class FloatCurve
    {
        public struct Key
        {
            public float Time;
            public float Value;

            public Key(float time, float value)
            {
                Time = time;
                Value = value;
            }
        }

        private Key[] _keys;
        private float[] _lut = Array.Empty<float>();
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

        public FloatCurve() : this(1f)
        {
        }

        public FloatCurve(float constant)
        {
            _keys = new[] { new Key(0f, constant), new Key(1f, constant) };
        }

        public static FloatCurve Constant(float value)
        {
            return new FloatCurve(value);
        }

        public void SetKeys(params Key[] keys)
        {
            if (keys == null || keys.Length == 0)
            {
                _keys = new[] { new Key(0f, 1f), new Key(1f, 1f) };
                _dirty = true;
                return;
            }

            var sanitized = new Key[keys.Length];
            for (var i = 0; i < keys.Length; i++)
            {
                var time = Clamp01(keys[i].Time);
                sanitized[i] = new Key(time, keys[i].Value);
            }

            Array.Sort(sanitized, (a, b) => a.Time.CompareTo(b.Time));

            if (sanitized.Length == 1)
            {
                _keys = new[] { sanitized[0], new Key(1f, sanitized[0].Value) };
            }
            else
            {
                _keys = sanitized;
            }

            _dirty = true;
        }

        public float Evaluate(float t)
        {
            EnsureLut();
            if (_lut.Length == 0) return 1f;

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
                _lut = Array.Empty<float>();
                return;
            }

            _lut = new float[_resolution];
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

                _lut[i] = a.Value + (b.Value - a.Value) * localT;
            }
        }

        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            return value > 1f ? 1f : value;
        }
    }
}
