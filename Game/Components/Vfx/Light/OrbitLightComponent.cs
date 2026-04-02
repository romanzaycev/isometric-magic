using System.Numerics;
using IsometricMagic.Game.Components.Spatial;
using IsometricMagic.Game.Model;

namespace IsometricMagic.Game.Components.Vfx.Light
{
    public class OrbitLightComponent : Component
    {
        private static readonly IEngineLogger Logger = Log.For<OrbitLightComponent>();

        private Light2D? _light;
        private CanvasPositionComponent? _canvasPosition;
        private CanvasPosition _center;
        private float _radius = 300f;
        private float _speed = 0.8f;
        private float _angle;
        private bool _missingCanvasPositionWarned;

        public override ComponentUpdateGroup UpdateGroup => ComponentUpdateGroup.Critical;

        public override int UpdateOrder => 50;

        public CanvasPosition Center
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
            _canvasPosition = Entity?.GetComponent<CanvasPositionComponent>();

            _light = new Light2D(_center.ToVector2())
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
            _light.Position = _center.ToVector2() + offset;

            if (_canvasPosition != null)
            {
                _canvasPosition.Position = CanvasPosition.FromVector2(_light.Position);
                return;
            }

            if (!_missingCanvasPositionWarned)
            {
                var entityName = Entity?.Name ?? "unknown";
                Logger.Warn($"OrbitLightComponent on '{entityName}' requires CanvasPositionComponent to move entity on canvas.");
                _missingCanvasPositionWarned = true;
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
