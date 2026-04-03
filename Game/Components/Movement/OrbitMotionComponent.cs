using System.Numerics;
using IsometricMagic.Game.Model;

namespace IsometricMagic.Game.Components.Movement
{
    public class OrbitMotionComponent : Component
    {
        private static readonly IEngineLogger Logger = Log.For<OrbitMotionComponent>();

        private CanvasPosition _center;
        private float _radius = 300f;
        private float _speed = 0.8f;
        private float _angle;

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
            Entity?.Transform.LocalPosition = GetOrbitPosition();
        }

        protected override void Update(float dt)
        {
            _angle += dt * _speed;
            Entity?.Transform.LocalPosition = GetOrbitPosition();
        }

        private Vector2 GetOrbitPosition()
        {
            var offset = new Vector2(MathF.Cos(_angle) * _radius, MathF.Sin(_angle) * _radius);
            
            return _center.ToVector2() + offset;
        }
    }
}
