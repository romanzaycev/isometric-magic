using System.Numerics;

namespace IsometricMagic.Game.Components.Rendering
{
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
        
        public int TextureWidth { get; set; }
        
        public int TextureHeight { get; set; }
        
        public OriginPoint OriginPoint { get; set; } = OriginPoint.LeftTop;
        
        public SpriteBlendMode BlendMode { get; set; } = SpriteBlendMode.Normal;
        
        public bool OutlineForceAlphaBlend { get; set; }
        
        public int Sorting { get; set; } = 0;
        
        public IMaterial? Material { get; set; }
        
        public Vector4? Color { get; set; }
        
        private Sprite? _sprite;
        
        private Texture? _texture;

        public override ComponentUpdateGroup UpdateGroup => ComponentUpdateGroup.Late;

        public override int UpdateOrder => 100;

        public Sprite? GetSprite() => _sprite;

        protected override void Awake()
        {
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

            var textureWidth = ResolveTextureWidth();
            var textureHeight = ResolveTextureHeight();
            _texture = new Texture(textureWidth, textureHeight);
            _texture.LoadImage(_imagePath);

            _sprite = new Sprite
            {
                Width = Width,
                Height = Height,
                Texture = _texture,
                OriginPoint = OriginPoint,
                BlendMode = BlendMode,
                Sorting = Sorting,
            };

            if (Material != null)
            {
                _sprite.Material = Material;
            }

            _sprite.Outline.ForceAlphaBlend = OutlineForceAlphaBlend;
            _sprite.Color = Color ?? new Vector4(1f, 1f, 1f, 1f);

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
            _sprite.BlendMode = BlendMode;
            _sprite.Outline.ForceAlphaBlend = OutlineForceAlphaBlend;

            if (Entity != null)
            {
                _sprite.Position = Entity.Transform.CanvasPosition;
            }
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

            var textureWidth = ResolveTextureWidth();
            var textureHeight = ResolveTextureHeight();
            _texture = new Texture(textureWidth, textureHeight);
            _texture.LoadImage(_imagePath);

            if (_sprite != null)
            {
                _sprite.Texture = _texture;
                _sprite.Width = Width;
                _sprite.Height = Height;
            }
        }

        private int ResolveTextureWidth()
        {
            return TextureWidth > 0 ? TextureWidth : Width;
        }

        private int ResolveTextureHeight()
        {
            return TextureHeight > 0 ? TextureHeight : Height;
        }
    }
}
