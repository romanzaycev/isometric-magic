using System.Numerics;
using IsometricMagic.Engine;
using IsometricMagic.Engine.Graphics.Lighting;

namespace IsometricMagic.Game.Components
{
    public class OrbitLightComponent : Component
    {
        private Light2D? _light;
        private Vector2 _center;
        private float _radius = 300f;
        private float _speed = 0.8f;
        private float _angle;

        public Vector2 Center
        {
            get => _center;
            set => _center = value;
        }

        public float Radius
        {
            get => _radius;
            set => _radius = value;
        }

        public float Speed
        {
            get => _speed;
            set => _speed = value;
        }

        protected override void Awake()
        {
            _light = new Light2D(_center)
            {
                Intensity = 2.5f,
                Radius = 256f,
                Height = 2f,
                Falloff = 2f,
                InnerRadius = 32f,
                CenterAttenuation = 0.5f,
                Color = new Vector3(1f, 0.1f, 0.1f),
            };
            Scene?.Lighting.Add(_light);
        }

        protected override void Update(float dt)
        {
            if (_light == null) return;

            _angle += dt * _speed;
            var offset = new Vector2(MathF.Cos(_angle) * _radius, MathF.Sin(_angle) * _radius);
            _light.Position = _center + offset;
        }

        protected override void OnDestroy()
        {
            if (_light != null)
            {
                Scene?.Lighting.Remove(_light);
            }
        }
    }
}
