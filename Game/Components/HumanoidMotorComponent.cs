using System;
using IsometricMagic.Engine;
using IsometricMagic.Game.Character;
using IsometricMagic.Game.Model;

namespace IsometricMagic.Game.Components
{
    public class HumanoidMotorComponent : Component
    {
        private const int MAX_MOVE = 5;

        private WorldDirection _direction = WorldDirection.N;
        private CharacterState _state = CharacterState.Idle;

        public WorldDirection Direction => _direction;
        public CharacterState State => _state;

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

        protected override void Update(float dt)
        {
            if (_positionComponent == null) return;

            var (moveX, moveY) = ReadInput();

            if (moveX == 0 && moveY == 0)
            {
                StopMove();
                return;
            }

            TryMove(moveX, moveY);
        }

        private (int moveX, int moveY) ReadInput()
        {
            var inputX = 0;
            var inputY = 0;

            if (Input.WasPressed(Key.W) || Input.WasPressed(Key.Up))
            {
                inputY -= 1;
            }
            if (Input.WasPressed(Key.S) || Input.WasPressed(Key.Down))
            {
                inputY += 1;
            }
            if (Input.WasPressed(Key.A) || Input.WasPressed(Key.Left))
            {
                inputX -= 1;
            }
            if (Input.WasPressed(Key.D) || Input.WasPressed(Key.Right))
            {
                inputX += 1;
            }

            return (inputX, inputY);
        }

        private void TryMove(int moveX, int moveY)
        {
            if (_converter == null || _positionComponent == null) return;

            var absMoveX = Math.Abs(moveX);
            var absMoveY = Math.Abs(moveY);

            if (absMoveX > 0)
            {
                moveX = moveX < 0 ? -Math.Min(absMoveX, MAX_MOVE) : Math.Min(absMoveX, MAX_MOVE);
            }

            if (absMoveY > 0)
            {
                moveY = moveY < 0 ? -Math.Min(absMoveY, MAX_MOVE) : Math.Min(absMoveY, MAX_MOVE);
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

        private void StopMove()
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
