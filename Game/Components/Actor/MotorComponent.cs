using System;
using IsometricMagic.Engine;
using IsometricMagic.Game.Model;
using IsometricMagic.Game.Components.Spatial;

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

        public void SetConverter(IsoWorldPositionConverter converter)
        {
            _converter = converter;
        }

        protected override void Awake()
        {
            _positionComponent = Entity?.GetComponent<WorldPositionComponent>();
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

            var nextXPos = _positionComponent.WorldPosX + moveX;
            var nextYPos = _positionComponent.WorldPosY + moveY;

            var moved = false;

            if (nextXPos >= IsoWorldPositionConverter.WORLD_BORDER_THRESHOLD &&
                nextXPos <= _converter.WorldWidth - IsoWorldPositionConverter.WORLD_BORDER_THRESHOLD)
            {
                if (_positionComponent.WorldPosX != nextXPos)
                {
                    _positionComponent.WorldPosX = nextXPos;
                    moved = true;
                }
            }

            if (nextYPos >= IsoWorldPositionConverter.WORLD_BORDER_THRESHOLD &&
                nextYPos <= _converter.WorldHeight - IsoWorldPositionConverter.WORLD_BORDER_THRESHOLD)
            {
                if (_positionComponent.WorldPosY != nextYPos)
                {
                    _positionComponent.WorldPosY = nextYPos;
                    moved = true;
                }
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
