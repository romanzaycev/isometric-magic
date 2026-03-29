using System;
using IsometricMagic.Engine;
using IsometricMagic.Game.Model;
using IsometricMagic.Game.Components.Spatial;
using IsometricMagic.Game.Components.Collision;

namespace IsometricMagic.Game.Components.Actor
{
    public class MotorComponent : Component
    {
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
            var nextXPos = currentX + moveX;
            var nextYPos = currentY + moveY;

            var moved = false;
            if (_collisionWorld != null && _collider != null)
            {
                if (TryMoveCandidate(nextXPos, nextYPos, applyX: true, applyY: true))
                {
                    moved = true;
                }
                else
                {
                    var movedX = moveX != 0 && TryMoveCandidate(nextXPos, currentY, applyX: true, applyY: false);
                    var movedY = !movedX && moveY != 0 && TryMoveCandidate(currentX, nextYPos, applyX: false, applyY: true);
                    moved = movedX || movedY;
                }
            }
            else
            {
                moved = TryMoveWithoutCollision(nextXPos, nextYPos);
            }

            if (moved)
            {
                _state = LocomotionState.Running;
                _direction = GetDirection(moveX, moveY);
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
