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
        public OriginPoint OriginPoint { get; set; } = OriginPoint.LeftTop;
        public int Sorting { get; set; } = 0;
        public IMaterial? Material { get; set; }
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
