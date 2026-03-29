using System;
using System.Numerics;
using IsometricMagic.Engine;
using IsometricMagic.Game.Model;
using IsometricMagic.Game.Components.Spatial;
using IsometricMagic.Game.Components.Collision;

namespace IsometricMagic.Game.Components.Actor
{
    public class MotorComponent : Component
    {
        private const float DirectionDeadband = 0.45f;
        private WorldDirection _direction = WorldDirection.N;
        private LocomotionState _state = LocomotionState.Idle;

        public WorldDirection Direction => _direction;
        public LocomotionState State => _state;

        public int MaxMove { get; set; } = 5;
        public IsoWorldPositionConverter? Converter => _converter;
        public WorldPositionComponent? PositionComponent => _positionComponent;

        private WorldPositionComponent? _positionComponent;
        private IsoWorldPositionConverter? _converter;
        private CollisionWorldComponent? _collisionWorld;
        private WorldColliderComponent? _collider;
        private Vector2 _floatPosition;
        private bool _floatPositionReady;
        private int _lastIntX;
        private int _lastIntY;
        private bool _hasLastInt;
        private Vector2 _lastMoveDelta;

        public void SetConverter(IsoWorldPositionConverter converter)
        {
            _converter = converter;
        }

        protected override void Awake()
        {
            _positionComponent = Entity?.GetComponent<WorldPositionComponent>();
            _collisionWorld = Scene?.FindComponent<CollisionWorldComponent>();
            _collider = Entity?.GetComponent<WorldColliderComponent>();
        }

        public void TryMove(int moveX, int moveY)
        {
            if (_converter == null || _positionComponent == null) return;

            if (moveX == 0 && moveY == 0)
            {
                StopMove();
                return;
            }

            var absMoveX = Math.Abs(moveX);
            var absMoveY = Math.Abs(moveY);

            if (absMoveX > 0)
            {
                moveX = moveX < 0 ? -Math.Min(absMoveX, MaxMove) : Math.Min(absMoveX, MaxMove);
            }

            if (absMoveY > 0)
            {
                moveY = moveY < 0 ? -Math.Min(absMoveY, MaxMove) : Math.Min(absMoveY, MaxMove);
            }

            var currentX = _positionComponent.WorldPosX;
            var currentY = _positionComponent.WorldPosY;
            var desired = new Vector2(moveX, moveY);

            var moved = false;
            var usedCollision = _collisionWorld != null && _collider != null;
            if (_collisionWorld != null && _collider != null)
            {
                moved = TryMoveWithSlide(currentX, currentY, desired);
            }
            else
            {
                var nextXPos = currentX + moveX;
                var nextYPos = currentY + moveY;
                moved = TryMoveWithoutCollision(nextXPos, nextYPos);
            }

            if (moved)
            {
                _state = LocomotionState.Running;
                var actualX = _positionComponent.WorldPosX - currentX;
                var actualY = _positionComponent.WorldPosY - currentY;
                if (!usedCollision)
                {
                    _lastMoveDelta = new Vector2(actualX, actualY);
                }

                var dirX = ApplyDeadband(_lastMoveDelta.X);
                var dirY = ApplyDeadband(_lastMoveDelta.Y);
                if (dirX != 0 || dirY != 0)
                {
                    _direction = GetDirection(dirX, dirY);
                }
                else if (actualX != 0 || actualY != 0)
                {
                    _direction = GetDirection(actualX, actualY);
                }
            }
            else
            {
                StopMove();
            }
        }

        public void StopMove()
        {
            _state = LocomotionState.Idle;
        }

        private bool TryMoveCandidate(int targetX, int targetY, bool applyX, bool applyY)
        {
            if (_converter == null || _positionComponent == null) return false;
            if (!IsWithinBounds(targetX, targetY, applyX, applyY)) return false;
            if (_collisionWorld == null || _collider == null) return false;

            var center = _collider.GetWorldCenter(targetX, targetY);
            if (_collisionWorld.OverlapsCircle(center, _collider.Radius, _collider))
            {
                return false;
            }

            var moved = false;
            if (applyX && _positionComponent.WorldPosX != targetX)
            {
                _positionComponent.WorldPosX = targetX;
                moved = true;
            }

            if (applyY && _positionComponent.WorldPosY != targetY)
            {
                _positionComponent.WorldPosY = targetY;
                moved = true;
            }

            return moved;
        }

        private bool TryMoveCandidate(Vector2 target, bool applyX, bool applyY)
        {
            var targetX = (int)MathF.Round(target.X);
            var targetY = (int)MathF.Round(target.Y);
            return TryMoveCandidate(targetX, targetY, applyX, applyY);
        }

        private bool TryMoveWithSlide(int currentX, int currentY, Vector2 desired)
        {
            if (_collisionWorld == null || _collider == null) return false;
            if (_converter == null || _positionComponent == null) return false;

            EnsureFloatPosition(currentX, currentY);
            var startPos = _floatPosition;
            var remaining = desired;
            var iterations = 4;

            for (var i = 0; i < iterations; i++)
            {
                if (remaining.LengthSquared() < 0.0001f)
                {
                    break;
                }

                remaining = ClampDeltaToBounds(_floatPosition, remaining);
                if (remaining.LengthSquared() < 0.0001f)
                {
                    break;
                }

                var startCenter = _floatPosition + _collider.Offset;
                if (!_collisionWorld.CastCircle(startCenter, remaining, _collider.Radius, _collider,
                        out _, out var t, out var normal))
                {
                    _floatPosition += remaining;
                    break;
                }

                if (t > 0f)
                {
                    _floatPosition += remaining * t;
                }

                var skin = _collisionWorld.ContactSkin + 0.001f;
                _floatPosition += normal * skin;

                var leftover = remaining * (1f - t);
                var into = Vector2.Dot(leftover, normal);
                if (into < 0f)
                {
                    leftover -= normal * into;
                }

                remaining = leftover;
            }

            _floatPosition = ClampToBounds(_floatPosition);
            var movedFloat = Vector2.DistanceSquared(startPos, _floatPosition) > 0.0001f;
            var movedInt = SyncFloatToWorld();
            _lastMoveDelta = _floatPosition - startPos;
            return movedFloat || movedInt;
        }

        private static int ApplyDeadband(float value)
        {
            if (MathF.Abs(value) < DirectionDeadband)
            {
                return 0;
            }

            return value > 0f ? 1 : -1;
        }

        private bool TryMoveWithoutCollision(int targetX, int targetY)
        {
            if (_converter == null || _positionComponent == null) return false;

            var moved = false;

            if (targetX >= IsoWorldPositionConverter.WORLD_BORDER_THRESHOLD &&
                targetX <= _converter.WorldWidth - IsoWorldPositionConverter.WORLD_BORDER_THRESHOLD)
            {
                if (_positionComponent.WorldPosX != targetX)
                {
                    _positionComponent.WorldPosX = targetX;
                    moved = true;
                }
            }

            if (targetY >= IsoWorldPositionConverter.WORLD_BORDER_THRESHOLD &&
                targetY <= _converter.WorldHeight - IsoWorldPositionConverter.WORLD_BORDER_THRESHOLD)
            {
                if (_positionComponent.WorldPosY != targetY)
                {
                    _positionComponent.WorldPosY = targetY;
                    moved = true;
                }
            }

            return moved;
        }

        private void EnsureFloatPosition(int currentX, int currentY)
        {
            if (!_floatPositionReady || !_hasLastInt || currentX != _lastIntX || currentY != _lastIntY)
            {
                _floatPosition = new Vector2(currentX, currentY);
                _floatPositionReady = true;
            }

            _lastIntX = currentX;
            _lastIntY = currentY;
            _hasLastInt = true;
        }

        private Vector2 ClampDeltaToBounds(Vector2 pos, Vector2 delta)
        {
            if (_converter == null) return delta;
            var target = pos + delta;
            var clamped = ClampToBounds(target);
            return clamped - pos;
        }

        private Vector2 ClampToBounds(Vector2 pos)
        {
            if (_converter == null) return pos;

            var min = IsoWorldPositionConverter.WORLD_BORDER_THRESHOLD;
            var maxX = _converter.WorldWidth - IsoWorldPositionConverter.WORLD_BORDER_THRESHOLD;
            var maxY = _converter.WorldHeight - IsoWorldPositionConverter.WORLD_BORDER_THRESHOLD;

            pos.X = MathF.Max(min, MathF.Min(maxX, pos.X));
            pos.Y = MathF.Max(min, MathF.Min(maxY, pos.Y));
            return pos;
        }

        private bool SyncFloatToWorld()
        {
            if (_positionComponent == null) return false;

            var intX = (int)MathF.Round(_floatPosition.X);
            var intY = (int)MathF.Round(_floatPosition.Y);
            var moved = false;

            if (_positionComponent.WorldPosX != intX)
            {
                _positionComponent.WorldPosX = intX;
                moved = true;
            }

            if (_positionComponent.WorldPosY != intY)
            {
                _positionComponent.WorldPosY = intY;
                moved = true;
            }

            _lastIntX = _positionComponent.WorldPosX;
            _lastIntY = _positionComponent.WorldPosY;
            _hasLastInt = true;
            return moved;
        }

        private bool IsWithinBounds(int targetX, int targetY, bool applyX, bool applyY)
        {
            if (_converter == null) return false;

            if (applyX)
            {
                if (targetX < IsoWorldPositionConverter.WORLD_BORDER_THRESHOLD ||
                    targetX > _converter.WorldWidth - IsoWorldPositionConverter.WORLD_BORDER_THRESHOLD)
                {
                    return false;
                }
            }

            if (applyY)
            {
                if (targetY < IsoWorldPositionConverter.WORLD_BORDER_THRESHOLD ||
                    targetY > _converter.WorldHeight - IsoWorldPositionConverter.WORLD_BORDER_THRESHOLD)
                {
                    return false;
                }
            }

            return true;
        }

        private static WorldDirection GetDirection(int moveX, int moveY)
        {
            if (moveY < 0 && moveX == 0) return WorldDirection.SW;
            if (moveY > 0 && moveX == 0) return WorldDirection.NE;
            if (moveY == 0 && moveX < 0) return WorldDirection.NW;
            if (moveY == 0 && moveX > 0) return WorldDirection.SE;
            if (moveY > 0 && moveX > 0) return WorldDirection.E;
            if (moveY > 0 && moveX < 0) return WorldDirection.N;
            if (moveY < 0 && moveX > 0) return WorldDirection.S;
            return WorldDirection.W;
        }
    }
}
