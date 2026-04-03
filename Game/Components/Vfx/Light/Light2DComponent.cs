using System.Numerics;

namespace IsometricMagic.Game.Components.Vfx.Light
{
    public class Light2DComponent : Component
    {
        private static readonly IEngineLogger Logger = Log.For<Light2DComponent>();

        private Light2D? _light;

        private Vector3 _color = new(1f, 1f, 1f);
        private float _intensity = 1f;
        private float _radius = 1024f;
        private float _height = 1.5f;
        private float _falloff = 2f;
        private float _innerRadius = 64f;
        private float _centerAttenuation = 0.75f;

        public Vector3 Color
        {
            get => _color;
            set
            {
                _color = value;
                if (_light != null)
                {
                    _light.Color = value;
                }
            }
        }

        public float Intensity
        {
            get => _intensity;
            set
            {
                _intensity = value;
                if (_light != null)
                {
                    _light.Intensity = value;
                }
            }
        }

        public float Radius
        {
            get => _radius;
            set
            {
                _radius = value;
                if (_light != null)
                {
                    _light.Radius = value;
                }
            }
        }

        public float Height
        {
            get => _height;
            set
            {
                _height = value;
                if (_light != null)
                {
                    _light.Height = value;
                }
            }
        }

        public float Falloff
        {
            get => _falloff;
            set
            {
                _falloff = value;
                if (_light != null)
                {
                    _light.Falloff = value;
                }
            }
        }

        public float InnerRadius
        {
            get => _innerRadius;
            set
            {
                _innerRadius = value;
                if (_light != null)
                {
                    _light.InnerRadius = value;
                }
            }
        }

        public float CenterAttenuation
        {
            get => _centerAttenuation;
            set
            {
                _centerAttenuation = value;
                if (_light != null)
                {
                    _light.CenterAttenuation = value;
                }
            }
        }
        
        protected override void Awake()
        {
            _light = new Light2D(GetCurrentCanvasPosition())
            {
                Color = _color,
                Intensity = _intensity,
                Radius = _radius,
                Height = _height,
                Falloff = _falloff,
                InnerRadius = _innerRadius,
                CenterAttenuation = _centerAttenuation,
            };
        }

        protected override void OnEnable()
        {
            if (_light != null)
            {
                Scene?.Lighting.Add(_light);
            }
        }

        protected override void OnDisable()
        {
            if (_light != null)
            {
                Scene?.Lighting.Remove(_light);
            }
        }

        protected override void Update(float dt)
        {
            if (_light == null)
            {
                return;
            }

            _light.Position = GetCurrentCanvasPosition();
        }

        protected override void OnDestroy()
        {
            if (_light != null)
            {
                Scene?.Lighting.Remove(_light);
                _light = null;
            }
        }


        private Vector2 GetCurrentCanvasPosition()
        {
            return Entity?.Transform.CanvasPosition ?? Vector2.Zero;
        }
    }
}
