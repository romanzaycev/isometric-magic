using System;
using IsometricMagic.Engine;
using IsometricMagic.Game.Model;

namespace IsometricMagic.Game.Components
{
    public class MotorComponent : Component
    {
        private WorldDirection _direction = WorldDirection.N;
        private CharacterState _state = CharacterState.Idle;

        public WorldDirection Direction => _direction;
        public CharacterState State => _state;

        public int MaxMove { get; set; } = 5;
        public IsoWorldPositionConverter? Converter => _converter;
        public WorldPositionComponent? PositionComponent => _positionComponent;

        private WorldPositionComponent? _positionComponent;
        private HumanoidAnimationComponent? _animationComponent;
        private IsoWorldPositionConverter? _converter;

        public void SetConverter(IsoWorldPositionConverter converter)
        {
            _converter = converter;
        }

        protected override void Awake()
        {
            _positionComponent = Entity?.GetComponent<WorldPositionComponent>();
            _animationComponent = Entity?.GetComponent<HumanoidAnimationComponent>();
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
                _state = CharacterState.Running;
                _direction = GetDirection(moveX, moveY);
                if (_animationComponent != null)
                {
                    _animationComponent.Direction = _direction;
                    _animationComponent.State = _state;
                }
            }
            else
            {
                StopMove();
            }
        }

        public void StopMove()
        {
            if (_state != CharacterState.Idle)
            {
                _state = CharacterState.Idle;
                if (_animationComponent != null)
                {
                    _animationComponent.State = _state;
                }
            }
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
