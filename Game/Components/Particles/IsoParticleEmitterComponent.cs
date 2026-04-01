using System.Numerics;
using IsometricMagic.Engine;
using IsometricMagic.Engine.Particles;
using IsometricMagic.Game.Components.Spatial;
using IsometricMagic.Game.Model;
using IsometricMagic.Game.Rendering;

namespace IsometricMagic.Game.Components.Particles
{
    public class IsoParticleEmitterComponent : Component
    {
        public int Bias { get; set; } = IsoSort.BiasVfx;
        public int LayerBase { get; set; }
        public bool UseIsoPosition { get; set; } = true;

        private IsoWorldPositionComponent? _worldPosition;
        private ParticleSystemComponent? _particleSystem;
        private IsoWorldPositionConverter? _converter;
        private bool _converterResolved;
        private int _lastBaseSorting;

        protected override void Awake()
        {
            _worldPosition = Entity?.GetComponent<IsoWorldPositionComponent>();
            _particleSystem = Entity?.GetComponent<ParticleSystemComponent>();
            _lastBaseSorting = int.MinValue;
        }

        protected override void LateUpdate(float dt)
        {
            if (_particleSystem == null)
            {
                return;
            }

            Vector2 canvasPos;
            if (UseIsoPosition && _worldPosition != null)
            {
                EnsureConverter();
                if (_converter != null)
                {
                    canvasPos = _converter.ToCanvas(_worldPosition.Position).ToVector2();
                }
                else
                {
                    canvasPos = _particleSystem.Position;
                }
            }
            else
            {
                canvasPos = _particleSystem.Position;
            }

            if (UseIsoPosition)
            {
                _particleSystem.UseEntityTransform = false;
                _particleSystem.Position = canvasPos;
            }

            var baseSorting = IsoSort.FromCanvas(CanvasPosition.FromVector2(canvasPos), LayerBase, Bias);
            if (baseSorting != _lastBaseSorting)
            {
                _particleSystem.SetBaseSorting(baseSorting, applyToAlive: true);
                _lastBaseSorting = baseSorting;
            }
        }

        private void EnsureConverter()
        {
            if (_converterResolved)
            {
                return;
            }

            _converterResolved = true;
            var provider = Scene?.FindComponent<WorldPositionConverterProviderComponent>();
            _converter = provider?.Converter;
        }
    }
}
