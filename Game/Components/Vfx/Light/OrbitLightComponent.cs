using System.Diagnostics;
using System.Numerics;
using IsometricMagic.Engine;
using IsometricMagic.Engine.Graphics.Lighting;
using IsometricMagic.Game.Components.Spatial;

namespace IsometricMagic.Game.Components.Vfx.Light
{
    public class OrbitLightComponent : Component
    {
        private Light2D? _light;
        private Vector2 _center;
        private float _radius = 300f;
        private float _speed = 0.8f;
        private float _angle;
        private WorldPositionComponent? _worldPosition;

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
            _worldPosition = Entity?.GetComponent<WorldPositionComponent>();
            _light = new Light2D(_center)
            {
                Intensity = 5f,
                Radius = 256f,
                Height = 2f,
                Falloff = 2f,
                InnerRadius = 64f,
                CenterAttenuation = 0.15f,
                Color = new Vector3(0.929f, 0.565f, 0.122f),
            };
            Scene?.Lighting.Add(_light);
        }

        protected override void Update(float dt)
        {
            if (_light == null) return;

            _angle += dt * _speed;
            var offset = new Vector2(MathF.Cos(_angle) * _radius, MathF.Sin(_angle) * _radius);
            _light.Position = _center + offset;

            if (_worldPosition != null)
            {
                _worldPosition.WorldPosX = (int)_light.Position.X;
                _worldPosition.WorldPosY = (int)_light.Position.Y;
            }
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
