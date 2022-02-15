using System;
using IsometricMagic.Game.Character;
using IsometricMagic.Game.Model;

namespace IsometricMagic.Game.Controllers.Character
{
    public abstract class CharacterMovementController
    {
        protected const int MAX_MOVE = 5;
        
        public abstract void HandleMovement(Game.Character.Character character, IsoWorldPositionConverter positionConverter);
        
        protected static WorldDirection GetDirection(Game.Character.Character human, int moveX, int moveY)
        {
            if (moveX == 0 && moveY == 0)
            {
                return human.Direction;
            }

            if (moveY < 0 && moveX == 0)
            {
                return WorldDirection.SW;
            }

            if (moveY > 0 && moveX == 0)
            {
                return WorldDirection.NE;
            }
            
            if (moveY == 0 && moveX < 0)
            {
                return WorldDirection.NW;
            }

            if (moveY == 0 && moveX > 0)
            {
                return WorldDirection.SE;
            }

            // --
            
            if (moveY > 0 && moveX > 0)
            {
                return WorldDirection.E;
            }
            
            if (moveY > 0 && moveX < 0)
            {
                return WorldDirection.N;
            }
            
            if (moveY < 0 && moveX > 0)
            {
                return WorldDirection.S;
            }
            
            return WorldDirection.W;
        }

        protected static void TryMove(int moveX, int moveY, Game.Character.Character character, IsoWorldPositionConverter positionConverter)
        {
            var absMoveX = Math.Abs(moveX);
            var absMoveY = Math.Abs(moveY);

            if (Math.Abs(moveX) > 0)
            {
                moveX = (moveX < 0) ? -Math.Min(absMoveX, MAX_MOVE) : Math.Min(absMoveX, MAX_MOVE);
            }
            
            if (Math.Abs(moveY) > 0)
            {
                moveY = (moveY < 0) ? -Math.Min(absMoveY, MAX_MOVE) : Math.Min(absMoveY, MAX_MOVE);
            }
            
            var nextXPos = character.WorldPosX + moveX;
            var nextYPos = character.WorldPosY + moveY;
            var isMoving = false;

            if (nextXPos >= IsoWorldPositionConverter.WORLD_BORDER_THRESHOLD && nextXPos <= positionConverter.WorldWidth - IsoWorldPositionConverter.WORLD_BORDER_THRESHOLD)
            {
                if (character.WorldPosX != nextXPos)
                {
                    character.WorldPosX = nextXPos;
                    isMoving = true;
                    character.Direction = GetDirection(character, moveX, moveY);
                }
            }
            
            if (nextYPos >= IsoWorldPositionConverter.WORLD_BORDER_THRESHOLD && nextYPos <= positionConverter.WorldHeight - IsoWorldPositionConverter.WORLD_BORDER_THRESHOLD)
            {
                if (character.WorldPosY != nextYPos)
                {
                    character.WorldPosY = nextYPos;
                    isMoving = true;
                    character.Direction = GetDirection(character, moveX, moveY);
                }
            }

            if (isMoving)
            {
                character.State = CharacterState.Running;
            }
            else
            {
                StopMove(character);
            }
        }

        protected static void StopMove(Game.Character.Character character)
        {
            character.State = CharacterState.Idle;
        }
    }
}