using IsometricMagic.Engine;
using IsometricMagic.Game.Model;
using SDL2;

namespace IsometricMagic.Game.Controllers.Character
{
    public class Keyboard : CharacterMovementController
    {
        public override void HandleMovement(Game.Character.Character character, IsoWorldPositionConverter positionConverter)
        {
            var moveX = 0;
            var moveY = 0;
            
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

            TryMove(moveX, moveY, character, positionConverter);
        }
    }
}