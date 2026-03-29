using System;
using System.Collections.Generic;
using System.Numerics;

namespace IsometricMagic.Engine.Tweening
{
    public class TweenManager
    {
        private readonly Dictionary<int, ITween> _tweens = new();
        private readonly List<int> _removeBuffer = new();
        private int _nextId = 1;

        public TweenHandle Delay(float seconds, Action? onComplete = null)
        {
            if (seconds <= 0f)
            {
                onComplete?.Invoke();
                return TweenHandle.Invalid;
            }

            return Add(new DelayTween(seconds, onComplete));
        }

        public TweenHandle To(Func<float> getter, Action<float> setter, float to, float duration,
            float delay = 0f, EaseFunc? ease = null, Action? onComplete = null)
        {
            return AddValueTween(getter, setter, to, duration, delay, ease, onComplete, Interp.Lerp);
        }

        public TweenHandle To(Func<double> getter, Action<double> setter, double to, float duration,
            float delay = 0f, EaseFunc? ease = null, Action? onComplete = null)
        {
            return AddValueTween(getter, setter, to, duration, delay, ease, onComplete, Interp.Lerp);
        }

        public TweenHandle To(Func<Vector2> getter, Action<Vector2> setter, Vector2 to, float duration,
            float delay = 0f, EaseFunc? ease = null, Action? onComplete = null)
        {
            return AddValueTween(getter, setter, to, duration, delay, ease, onComplete, Interp.Lerp);
        }

        public TweenHandle To(Func<Vector3> getter, Action<Vector3> setter, Vector3 to, float duration,
            float delay = 0f, EaseFunc? ease = null, Action? onComplete = null)
        {
            return AddValueTween(getter, setter, to, duration, delay, ease, onComplete, Interp.Lerp);
        }

        public void Update(float dt)
        {
            if (_tweens.Count == 0) return;

            _removeBuffer.Clear();

            foreach (var pair in _tweens)
            {
                if (pair.Value.Tick(dt))
                {
                    _removeBuffer.Add(pair.Key);
                }
            }

            if (_removeBuffer.Count == 0) return;

            foreach (var id in _removeBuffer)
            {
                _tweens.Remove(id);
            }
        }

        public void Clear()
        {
            foreach (var tween in _tweens.Values)
            {
                tween.Cancel();
            }
            _tweens.Clear();
        }

        internal void Cancel(int id)
        {
            if (_tweens.TryGetValue(id, out var tween))
            {
                tween.Cancel();
                _tweens.Remove(id);
            }
        }

        internal bool IsActive(int id)
        {
            return _tweens.ContainsKey(id);
        }

        private TweenHandle Add(ITween tween)
        {
            var id = _nextId++;
            _tweens.Add(id, tween);
            return new TweenHandle(this, id);
        }

        private TweenHandle AddValueTween<T>(Func<T> getter, Action<T> setter, T to, float duration,
            float delay, EaseFunc? ease, Action? onComplete, Func<T, T, float, T> lerp)
        {
            if (duration <= 0f && delay <= 0f)
            {
                setter(to);
                onComplete?.Invoke();
                return TweenHandle.Invalid;
            }

            var tween = new ValueTween<T>(getter, setter, to, duration, delay, ease ?? Easing.Linear, onComplete, lerp);
            return Add(tween);
        }

        private interface ITween
        {
            bool Tick(float dt);
            void Cancel();
        }

        private sealed class DelayTween : ITween
        {
            private readonly float _duration;
            private readonly Action? _onComplete;
            private float _elapsed;
            private bool _cancelled;

            public DelayTween(float duration, Action? onComplete)
            {
                _duration = duration;
                _onComplete = onComplete;
            }

            public bool Tick(float dt)
            {
                if (_cancelled) return true;

                _elapsed += dt;
                if (_elapsed < _duration) return false;

                _onComplete?.Invoke();
                return true;
            }

            public void Cancel()
            {
                _cancelled = true;
            }
        }

        private sealed class ValueTween<T> : ITween
        {
            private readonly Func<T> _getter;
            private readonly Action<T> _setter;
            private readonly T _to;
            private readonly float _duration;
            private readonly float _delay;
            private readonly EaseFunc _ease;
            private readonly Action? _onComplete;
            private readonly Func<T, T, float, T> _lerp;

            private T _from = default!;
            private float _elapsed;
            private bool _started;
            private bool _cancelled;

            public ValueTween(Func<T> getter, Action<T> setter, T to, float duration, float delay,
                EaseFunc ease, Action? onComplete, Func<T, T, float, T> lerp)
            {
                _getter = getter;
                _setter = setter;
                _to = to;
                _duration = duration;
                _delay = delay;
                _ease = ease;
                _onComplete = onComplete;
                _lerp = lerp;
            }

            public bool Tick(float dt)
            {
                if (_cancelled) return true;

                _elapsed += dt;
                if (!_started)
                {
                    if (_elapsed < _delay) return false;
                    _started = true;
                    _from = _getter();
                }

                var t = _duration <= 0f ? 1f : (_elapsed - _delay) / _duration;
                t = Interp.Clamp01(t);
                var eased = _ease(t);
                _setter(_lerp(_from, _to, eased));

                if (t >= 1f)
                {
                    _onComplete?.Invoke();
                    return true;
                }

                return false;
            }

            public void Cancel()
            {
                _cancelled = true;
            }
        }
    }
}
