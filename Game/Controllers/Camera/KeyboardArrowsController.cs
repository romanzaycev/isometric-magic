using IsometricMagic.Engine;
using SDL2;

namespace IsometricMagic.Game.Controllers.Camera
{
    public class KeyboardArrowsController : ICameraController
    {
        public void UpdateCamera(Engine.Camera camera)
        {
            if (Input.IsPressed(SDL.SDL_Keycode.SDLK_LEFT))
            {
                camera.Rect.X += 10;
            }
            
            if (Input.IsPressed(SDL.SDL_Keycode.SDLK_RIGHT))
            {
                camera.Rect.X -= 10;
            }
            
            if (Input.IsPressed(SDL.SDL_Keycode.SDLK_UP))
            {
                camera.Rect.Y += 10;
            }

            if (Input.IsPressed(SDL.SDL_Keycode.SDLK_DOWN))
            {
                camera.Rect.Y -= 10;
            }
        }
    }
}