using IsometricMagic.Game.Rendering;
using IsometricMagic.Game.Model;

namespace IsometricMagic.Game.Components.Character.Humanoid
{
    public class HumanoidCanvasPositionSyncComponent : Component
    {
        public int LayerBase { get; set; }

        private HumanoidAnimationComponent? _animationComponent;

        public override ComponentUpdateGroup UpdateGroup => ComponentUpdateGroup.Late;

        public override int UpdateOrder => 200;

        protected override void Awake()
        {
            _animationComponent = Entity?.GetComponent<HumanoidAnimationComponent>();
        }

        protected override void LateUpdate(float dt)
        {
            if (_animationComponent == null || Entity == null)
            {
                return;
            }

            var sprite = _animationComponent.GetCurrentSprite();
            if (sprite == null)
            {
                return;
            }

            var canvasPos = CanvasPosition.FromVector2(Entity.Transform.CanvasPosition);
            sprite.Position = canvasPos.ToVector2();
            var sorting = IsoSort.FromCanvas(canvasPos, LayerBase, IsoSort.BiasActor);
            if (sprite.Sorting != sorting)
            {
                sprite.Sorting = sorting;
            }
        }
    }
}
