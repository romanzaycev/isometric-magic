using IsometricMagic.Engine;
using IsometricMagic.Game.Model;
using IsometricMagic.Game.Components.Spatial;
using IsometricMagic.Game.Rendering;

namespace IsometricMagic.Game.Components.Character.Humanoid
{
    public class HumanoidWorldPositionSyncComponent : Component
    {
        public int LayerBase { get; set; }

        private IsoWorldPositionComponent? _positionComponent;
        private HumanoidAnimationComponent? _animationComponent;
        private IsoWorldPositionConverter? _converter;
        private bool _converterResolved;

        public void SetConverter(IsoWorldPositionConverter converter)
        {
            _converter = converter;
            _converterResolved = true;
        }

        protected override void Awake()
        {
            _positionComponent = Entity?.GetComponent<IsoWorldPositionComponent>();
            _animationComponent = Entity?.GetComponent<HumanoidAnimationComponent>();
        }

        protected override void LateUpdate(float dt)
        {
            EnsureConverter();
            if (_positionComponent == null || _animationComponent == null || _converter == null) return;

            var sprite = _animationComponent.GetCurrentSprite();
            if (sprite == null) return;

            var canvasPos = _converter.ToCanvas(_positionComponent.Position);
            sprite.Position = canvasPos.ToVector2();
            var sorting = IsoSort.FromCanvas(canvasPos, LayerBase, IsoSort.BiasActor);
            if (sprite.Sorting != sorting)
            {
                sprite.Sorting = sorting;
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
