using System.Numerics;
using IsometricMagic.Engine;
using IsometricMagic.Game.Components.Spatial;
using IsometricMagic.Game.Model;

namespace IsometricMagic.Game.Components.Collision
{
    public class WorldColliderComponent : Component
    {
        public float Radius { get; set; } = 20f;
        public Vector2 Offset { get; set; } = Vector2.Zero;
        public bool IsStatic { get; set; } = false;
        public bool DebugDraw { get; set; } = false;
        public Vector4 DebugColor { get; set; } = new(1f, 0f, 0f, 0.5f);
        public int DebugSorting { get; set; } = 1_900_000_000;

        private WorldPositionComponent? _worldPosition;
        private CollisionWorldComponent? _collisionWorld;
        private Vector2 _lastCenter;
        private bool _hasLastCenter;
        private IsoWorldPositionConverter? _converter;
        private bool _converterResolved;
        private Sprite? _debugSprite;
        private Texture? _debugTexture;

        protected override void Awake()
        {
            _worldPosition = Entity?.GetComponent<WorldPositionComponent>();
            _collisionWorld = Scene?.FindComponent<CollisionWorldComponent>();
        }

        protected override void OnEnable()
        {
            _collisionWorld?.Register(this);
            if (DebugDraw)
            {
                CreateDebugSprite();
            }
        }

        protected override void OnDisable()
        {
            _collisionWorld?.Unregister(this);
            if (_debugSprite != null)
            {
                _debugSprite.Visible = false;
            }
        }

        protected override void LateUpdate(float dt)
        {
            if (_collisionWorld != null && !IsStatic)
            {
                var center = GetWorldCenter();
                if (!_hasLastCenter || Vector2.DistanceSquared(center, _lastCenter) > 0.01f)
                {
                    _collisionWorld.UpdateCollider(this, center);
                    _lastCenter = center;
                    _hasLastCenter = true;
                }
            }

            UpdateDebugSprite();
        }

        protected override void OnDestroy()
        {
            _collisionWorld?.Unregister(this);
            DestroyDebugSprite();
        }

        public Vector2 GetWorldCenter(int? worldX = null, int? worldY = null)
        {
            if (_worldPosition != null)
            {
                var x = worldX ?? _worldPosition.WorldPosX;
                var y = worldY ?? _worldPosition.WorldPosY;
                return new Vector2(x, y) + Offset;
            }

            if (Entity != null)
            {
                return Entity.Transform.WorldPosition + Offset;
            }

            return Offset;
        }

        private void CreateDebugSprite()
        {
            if (_debugSprite != null) return;

            if (_debugTexture == null)
            {
                _debugTexture = new Texture(128, 128);
                _debugTexture.LoadImage("./resources/data/textures/loading_circle.png");
            }

            _debugSprite = new Sprite
            {
                Texture = _debugTexture,
                OriginPoint = OriginPoint.Centered,
                Color = DebugColor,
                Sorting = DebugSorting,
                Width = (int) MathF.Round(Radius * 2f),
                Height = (int) MathF.Round(Radius * 2f)
            };

            Scene?.MainLayer.Add(_debugSprite);
        }

        private void UpdateDebugSprite()
        {
            if (!DebugDraw) return;
            if (_debugSprite == null) return;

            EnsureConverter();
            var center = GetWorldCenter();
            var canvasPos = _converter != null
                ? _converter.GetCanvasPosition((int) center.X, (int) center.Y)
                : center;

            _debugSprite.Position = canvasPos;
            _debugSprite.Width = (int) MathF.Round(Radius * 2f);
            _debugSprite.Height = (int) MathF.Round(Radius * 2f);
            _debugSprite.Visible = true;
        }

        private void DestroyDebugSprite()
        {
            if (_debugSprite != null)
            {
                Scene?.MainLayer.Remove(_debugSprite);
                _debugSprite = null;
            }

            if (_debugTexture != null)
            {
                TextureHolder.GetInstance().Remove(_debugTexture);
                _debugTexture = null;
            }
        }

        private void EnsureConverter()
        {
            if (_converterResolved) return;
            _converterResolved = true;
            var provider = Scene?.FindComponent<WorldPositionConverterProviderComponent>();
            _converter = provider?.Converter;
        }
    }
}
