using System.Numerics;
using IsometricMagic.Engine;
using IsometricMagic.Game.Components.Spatial;
using IsometricMagic.Game.Model;
using IsometricMagic.Game.Rendering;

namespace IsometricMagic.Game.Components.Rendering
{
    public class IsoDepthSortComponent : Component
    {
        public int Bias { get; set; } = IsoSort.BiasObject;
        public int LayerBase { get; set; }

        private WorldPositionComponent? _worldPosition;
        private SpriteRendererComponent? _spriteRenderer;
        private IsoWorldPositionConverter? _converter;
        private bool _converterResolved;

        protected override void Awake()
        {
            _worldPosition = Entity?.GetComponent<WorldPositionComponent>();
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

            Vector2 canvasPos;
            if (_worldPosition != null)
            {
                EnsureConverter();
                if (_converter != null)
                {
                    canvasPos = _converter.GetCanvasPosition(_worldPosition.WorldPosX, _worldPosition.WorldPosY);
                }
                else
                {
                    canvasPos = sprite.Position;
                }
            }
            else
            {
                canvasPos = sprite.Position;
            }

            var sorting = IsoSort.FromCanvas(canvasPos, LayerBase, Bias);
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
