using System;
using System.Numerics;
using IsometricMagic.Engine;
using IsometricMagic.Engine.Graphics.Materials;
using IsometricMagic.Game.Components.Spatial;
using IsometricMagic.Game.Model;

namespace IsometricMagic.Game.Components.Rendering
{
    public enum SpritePositionMode
    {
        CanvasFromIsoWorldPositionComponent,
        CanvasFromEntityTransform
    }

    public class SpriteRendererComponent : Component
    {
        public SceneLayer? TargetLayer { get; set; }
        private string _imagePath = string.Empty;
        public string ImagePath
        {
            get => _imagePath;
            set
            {
                if (_imagePath == value) return;
                _imagePath = value;
                if (_sprite != null)
                {
                    ReloadTexture();
                }
            }
        }
        public int Width { get; set; }
        public int Height { get; set; }
        public OriginPoint OriginPoint { get; set; } = OriginPoint.LeftTop;
        public int Sorting { get; set; } = 0;
        public IMaterial? Material { get; set; }
        public SpritePositionMode PositionMode { get; set; } = SpritePositionMode.CanvasFromEntityTransform;

        private IsoWorldPositionComponent? _worldPosition;
        private IsoWorldPositionConverter? _converter;
        private Sprite? _sprite;
        private Texture? _texture;
        private bool _converterResolved;

        public void SetConverter(IsoWorldPositionConverter converter)
        {
            _converter = converter;
            _converterResolved = true;
        }

        public Sprite? GetSprite() => _sprite;

        protected override void Awake()
        {
            _worldPosition = Entity?.GetComponent<IsoWorldPositionComponent>();
            if (TargetLayer == null)
            {
                TargetLayer = Scene?.MainLayer;
            }

            if (TargetLayer == null)
            {
                return;
            }

            if (Width <= 0 || Height <= 0)
            {
                throw new InvalidOperationException("SpriteRendererComponent requires Width and Height to be set.");
            }

            if (string.IsNullOrWhiteSpace(_imagePath))
            {
                throw new InvalidOperationException("SpriteRendererComponent requires ImagePath to be set.");
            }

            _texture = new Texture(Width, Height);
            _texture.LoadImage(_imagePath);

            _sprite = new Sprite
            {
                Width = Width,
                Height = Height,
                Texture = _texture,
                OriginPoint = OriginPoint,
                Sorting = Sorting
            };

            if (Material != null)
            {
                _sprite.Material = Material;
            }

            TargetLayer.Add(_sprite);
        }

        protected override void OnEnable()
        {
            if (_sprite != null)
            {
                _sprite.Visible = true;
            }
        }

        protected override void OnDisable()
        {
            if (_sprite != null)
            {
                _sprite.Visible = false;
            }
        }

        protected override void LateUpdate(float dt)
        {
            if (_sprite == null)
            {
                return;
            }

            _sprite.Sorting = Sorting;

            if (PositionMode == SpritePositionMode.CanvasFromEntityTransform)
            {
                if (Entity != null)
                {
                    _sprite.Position = Entity.Transform.CanvasPosition;
                }
                return;
            }

            if (_worldPosition == null)
            {
                return;
            }

            if (PositionMode == SpritePositionMode.CanvasFromIsoWorldPositionComponent)
            {
                EnsureConverter();
                if (_converter == null)
                {
                    return;
                }

                var canvasPos = _converter.ToCanvas(_worldPosition.Position);
                _sprite.Position = canvasPos.ToVector2();
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

        protected override void OnDestroy()
        {
            if (_sprite != null)
            {
                TargetLayer?.Remove(_sprite);
            }

            if (_texture != null)
            {
                _texture.Destroy();
            }

            _sprite = null;
            _texture = null;
        }

        private void ReloadTexture()
        {
            if (string.IsNullOrWhiteSpace(_imagePath))
            {
                return;
            }

            if (_texture != null)
            {
                _texture.Destroy();
            }

            _texture = new Texture(Width, Height);
            _texture.LoadImage(_imagePath);

            if (_sprite != null)
            {
                _sprite.Texture = _texture;
                _sprite.Width = Width;
                _sprite.Height = Height;
            }
        }
    }
}
