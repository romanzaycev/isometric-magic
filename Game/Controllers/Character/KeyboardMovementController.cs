using System;
using IsometricMagic.Engine;
using IsometricMagic.Game.Character;
using IsometricMagic.Game.Model;
using SDL2;

namespace IsometricMagic.Game.Controllers.Character
{
    public class KeyboardMovementController : CharacterMovementController
    {
        public override void HandleMovement(Game.Character.Character character, IsoWorldPositionConverter positionConverter)
        {
            var moveX = 0;
            var moveY = 0;
            const int maxAbsMove = 5;
            
            if (Input.IsPressed(SDL.SDL_Keycode.SDLK_UP))
            {
                moveY = -5;
            }

            if (Input.IsPressed(SDL.SDL_Keycode.SDLK_DOWN))
            {
                moveY = 5;
            }
            
            if (Input.IsPressed(SDL.SDL_Keycode.SDLK_LEFT))
            {
                moveX = -5;
            }
            
            if (Input.IsPressed(SDL.SDL_Keycode.SDLK_RIGHT))
            {
                moveX = 5;
            }

            var absMoveX = Math.Abs(moveX);
            var absMoveY = Math.Abs(moveY);
            
            if (Math.Abs(moveX) > 0)
            {
                moveX = (moveX < 0) ? -Math.Min(absMoveX, maxAbsMove) : Math.Min(absMoveX, maxAbsMove);
            }
            
            if (Math.Abs(moveY) > 0)
            {
                moveY = (moveY < 0) ? -Math.Min(absMoveY, maxAbsMove) : Math.Min(absMoveY, maxAbsMove);
            }

            const int worldBorderThreshold = 30;
            var nextXPos = character.WorldPosX + moveX;
            var nextYPos = character.WorldPosY + moveY;
            var isMoving = false;

            if (nextXPos >= worldBorderThreshold && nextXPos <= positionConverter.WorldWidth - worldBorderThreshold)
            {
                if (character.WorldPosX != nextXPos)
                {
                    character.WorldPosX = nextXPos;
                    isMoving = true;
                    character.Direction = GetDirection(character, moveX, moveY);
                }
            }
            
            if (nextYPos >= worldBorderThreshold && nextYPos <= positionConverter.WorldHeight - worldBorderThreshold)
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
                character.State = CharacterState.Idle;
            }
        }
    }
}