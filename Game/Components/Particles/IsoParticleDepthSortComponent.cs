using IsometricMagic.Game.Model;
using IsometricMagic.Game.Rendering;

namespace IsometricMagic.Game.Components.Particles
{
    public class IsoParticleDepthSortComponent : Component
    {
        public int Bias { get; set; } = IsoSort.BiasVfx;
        public int LayerBase { get; set; }

        private ParticleSystemComponent? _particleSystem;
        private int _lastBaseSorting;

        public override ComponentUpdateGroup UpdateGroup => ComponentUpdateGroup.Late;

        public override int UpdateOrder => 300;

        protected override void Awake()
        {
            _particleSystem = Entity?.GetComponent<ParticleSystemComponent>();
            _lastBaseSorting = int.MinValue;

            if (_particleSystem != null)
            {
                _particleSystem.UseEntityTransform = true;
            }
        }

        protected override void LateUpdate(float dt)
        {
            if (_particleSystem == null)
            {
                return;
            }

            var canvasPos = Entity?.Transform.CanvasPosition ?? _particleSystem.Position;

            var baseSorting = IsoSort.FromCanvas(CanvasPosition.FromVector2(canvasPos), LayerBase, Bias);
            if (baseSorting != _lastBaseSorting)
            {
                _particleSystem.SetBaseSorting(baseSorting, applyToAlive: true);
                _lastBaseSorting = baseSorting;
            }
        }
    }
}
