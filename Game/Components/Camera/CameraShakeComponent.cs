using System.Numerics;

namespace IsometricMagic.Game.Components.Camera
{
    public class CameraShakeComponent : CameraInfluenceComponent
    {
        [RuntimeEditorEditable(Step = 0.1f)]
        public float Amplitude { get; private set; }
        
        [RuntimeEditorEditable(Step = 0.1f)]
        public float Frequency { get; private set; } = 12f;

        private float _duration;
        private float _remaining;
        private float _time;
        private float _phaseX;
        private float _phaseY;

        public void Play(float amplitude, float duration, float frequency = 12f)
        {
            if (duration <= 0f || amplitude <= 0f)
            {
                Stop();
                return;
            }

            Amplitude = amplitude;
            Frequency = frequency;
            _duration = duration;
            _remaining = duration;
            _time = 0f;
            _phaseX = (float) (Random.Shared.NextDouble() * MathF.PI * 2f);
            _phaseY = (float) (Random.Shared.NextDouble() * MathF.PI * 2f);
        }

        public void Stop()
        {
            _remaining = 0f;
            _duration = 0f;
            _time = 0f;
            Amplitude = 0f;
        }

        protected override void Update(float dt)
        {
            if (_remaining <= 0f) return;

            _time += dt;
            _remaining -= dt;

            if (_remaining <= 0f)
            {
                Stop();
            }
        }

        public override void CollectInfluence(List<CameraInfluence> buffer)
        {
            if (_remaining <= 0f || Amplitude <= 0f) return;

            var falloff = _duration > 0f ? _remaining / _duration : 0f;
            var intensity = Amplitude * falloff;
            var angle = _time * Frequency * MathF.PI * 2f;

            var offset = new Vector2(
                MathF.Sin(angle + _phaseX) * intensity,
                MathF.Cos(angle + _phaseY) * intensity
            );

            AddShake(buffer, offset, Priority);
        }
    }
}
