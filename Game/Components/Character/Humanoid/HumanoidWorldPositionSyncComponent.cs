using IsometricMagic.Engine;
using IsometricMagic.Game.Model;
using IsometricMagic.Game.Components.Spatial;
using IsometricMagic.Game.Rendering;

namespace IsometricMagic.Game.Components.Character.Humanoid
{
    public class HumanoidWorldPositionSyncComponent : Component
    {
        public int LayerBase { get; set; }

        private WorldPositionComponent? _positionComponent;
        private HumanoidAnimationComponent? _animationComponent;
        private IsoWorldPositionConverter? _converter;

        public void SetConverter(IsoWorldPositionConverter converter)
        {
            _converter = converter;
        }

        protected override void Awake()
        {
            _positionComponent = Entity?.GetComponent<WorldPositionComponent>();
            _animationComponent = Entity?.GetComponent<HumanoidAnimationComponent>();
        }

        protected override void LateUpdate(float dt)
        {
            if (_positionComponent == null || _animationComponent == null || _converter == null) return;

            var sprite = _animationComponent.GetCurrentSprite();
            if (sprite == null) return;

            var pos = _converter.GetCanvasPosition(_positionComponent.WorldPosX, _positionComponent.WorldPosY);
            sprite.Position = pos;
            var sorting = IsoSort.FromCanvas(pos, LayerBase, IsoSort.BiasActor);
            if (sprite.Sorting != sorting)
            {
                sprite.Sorting = sorting;
            }
        }
    }
}
