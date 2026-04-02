using System.Numerics;
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

        private IsoWorldPositionComponent? _worldPosition;
        private CollisionWorldComponent? _collisionWorld;
        private Vector2 _lastCenter;
        private bool _hasLastCenter;
        private IsoWorldPositionConverter? _converter;
        private bool _converterResolved;
        private Sprite? _debugSprite;
        private Texture? _debugTexture;
        private bool _isRegistered;

        protected override void Awake()
        {
            _collisionWorld = Scene?.FindComponent<CollisionWorldComponent>();
        }

        protected override void OnEnable()
        {
            TryRegister();
            if (DebugDraw)
            {
                CreateDebugSprite();
            }
        }

        protected override void OnDisable()
        {
            UnregisterIfNeeded();
            if (_debugSprite != null)
            {
                _debugSprite.Visible = false;
            }
        }

        protected override void LateUpdate(float dt)
        {
            if (_collisionWorld == null) return;
            ResolveWorldPositionComponent();
            if (_worldPosition == null) return;

            TryRegister();

            var center = GetWorldCenter();
            var positionChanged = !_hasLastCenter || Vector2.DistanceSquared(center, _lastCenter) > 0.01f;

            if (positionChanged)
            {
                _collisionWorld.UpdateCollider(this, center);
            }

            if (positionChanged)
            {
                _lastCenter = center;
                _hasLastCenter = true;
            }

            UpdateDebugSprite();
        }

        protected override void OnDestroy()
        {
            UnregisterIfNeeded();
            DestroyDebugSprite();
        }

        public Vector2 GetWorldCenter(float? worldX = null, float? worldY = null)
        {
            ResolveWorldPositionComponent();
            if (_worldPosition == null)
            {
                throw new InvalidOperationException("WorldColliderComponent requires IsoWorldPositionComponent.");
            }

            var x = worldX ?? _worldPosition.X;
            var y = worldY ?? _worldPosition.Y;
            return new Vector2(x, y) + Offset;
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
                Width = 128,
                Height = 128
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
                ? _converter.ToCanvas(IsoWorldPosition.FromVector2(center)).ToVector2()
                : center;

            _debugSprite.Position = canvasPos;
            _debugSprite.Visible = true;
            if (_converter == null)
            {
                var size = MathF.Max(1f, Radius * 2f);
                _debugSprite.Width = (int)MathF.Round(size);
                _debugSprite.Height = (int)MathF.Round(size);
                _debugSprite.Transformation.Rotation.Clockwise = true;
                _debugSprite.Transformation.Rotation.Angle = 0f;
                return;
            }

            var centerCanvas = _converter.ToCanvas(IsoWorldPosition.FromVector2(center)).ToVector2();
            var axisX = _converter.ToCanvas(IsoWorldPosition.FromVector2(center + new Vector2(1f, 0f))).ToVector2() - centerCanvas;
            var axisY = _converter.ToCanvas(IsoWorldPosition.FromVector2(center + new Vector2(0f, 1f))).ToVector2() - centerCanvas;

            var q11 = axisX.X * axisX.X + axisY.X * axisY.X;
            var q12 = axisX.X * axisX.Y + axisY.X * axisY.Y;
            var q22 = axisX.Y * axisX.Y + axisY.Y * axisY.Y;

            var trace = q11 + q22;
            var det = q11 * q22 - q12 * q12;
            var halfTrace = trace * 0.5f;
            var disc = MathF.Max(0f, halfTrace * halfTrace - det);
            var sqrtDisc = MathF.Sqrt(disc);
            var lambdaMax = halfTrace + sqrtDisc;
            var lambdaMin = MathF.Max(0f, halfTrace - sqrtDisc);

            var semiMax = Radius * MathF.Sqrt(lambdaMax);
            var semiMin = Radius * MathF.Sqrt(lambdaMin);

            var width = MathF.Max(1f, semiMax * 2f);
            var height = MathF.Max(1f, semiMin * 2f);

            _debugSprite.Width = (int)MathF.Round(width);
            _debugSprite.Height = (int)MathF.Round(height);

            var angle = 0f;
            if (MathF.Abs(q12) > 0.00001f || MathF.Abs(q11 - lambdaMax) > 0.00001f)
            {
                var vx = lambdaMax - q22;
                var vy = q12;
                var len = MathF.Sqrt(vx * vx + vy * vy);
                if (len > 0.00001f)
                {
                    vx /= len;
                    vy /= len;
                    angle = MathF.Atan2(vy, vx);
                }
            }
            else if (q22 > q11)
            {
                angle = MathF.PI * 0.5f;
            }

            _debugSprite.Transformation.Rotation.Clockwise = false;
            _debugSprite.Transformation.Rotation.Angle = angle / (MathF.PI * 2f);
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
                _debugTexture.Destroy();
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

        private void ResolveWorldPositionComponent()
        {
            if (_worldPosition != null) return;
            _worldPosition = Entity?.GetComponent<IsoWorldPositionComponent>();
        }

        private void TryRegister()
        {
            if (_isRegistered || _collisionWorld == null) return;
            ResolveWorldPositionComponent();
            if (_worldPosition == null) return;

            _collisionWorld.Register(this);
            _isRegistered = true;
        }

        private void UnregisterIfNeeded()
        {
            if (!_isRegistered) return;
            _collisionWorld?.Unregister(this);
            _isRegistered = false;
        }
    }
}
