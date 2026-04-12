using IonMotion.Game.Model;
using IonMotion.Game.Rendering;

namespace IonMotion.Game.Components.Rendering
{
    public class IsoDepthSortComponent : Component
    {
        public int Bias { get; set; } = IsoSort.BiasObject;
        public int LayerBase { get; set; }

        private SpriteRendererComponent? _spriteRenderer;

        public override ComponentUpdateGroup UpdateGroup => ComponentUpdateGroup.Late;

        public override int UpdateOrder => 500;

        protected override void Awake()
        {
            _spriteRenderer = Entity?.GetComponent<SpriteRendererComponent>();
        }

        protected override void LateUpdate(float dt)
        {
            if (_spriteRenderer == null)
            {
                return;
            }

            var sprite = _spriteRenderer.GetSprite();
            if (sprite == null)
            {
                return;
            }

            var canvasPos = Entity?.Transform.CanvasPosition ?? sprite.Position;

            var sorting = IsoSort.FromCanvas(CanvasPosition.FromVector2(canvasPos), LayerBase, Bias);
            if (sprite.Sorting != sorting)
            {
                sprite.Sorting = sorting;
            }
        }
    }
}
