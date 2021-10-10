using System;
using SDL2;

namespace IsometricMagic
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Renderer renderer = Renderer.GetInstance();
                renderer.Init();

                SDL.SDL_Event sdlEvent;
                bool isRunning = true;

                while (isRunning)
                {
                    while (SDL.SDL_PollEvent(out sdlEvent) != 0)
                    {
                        switch (sdlEvent.type)
                        {
                            case SDL.SDL_EventType.SDL_WINDOWEVENT:
                                renderer.HandleWindowEvent(sdlEvent);
                                break;

                            case SDL.SDL_EventType.SDL_QUIT:
                                isRunning = false;
                                break;

                            case SDL.SDL_EventType.SDL_KEYDOWN:
                                switch (sdlEvent.key.keysym.sym)
                                {
                                    case SDL.SDL_Keycode.SDLK_ESCAPE:
                                        isRunning = false;
                                        break;
                                }

                                break;
                        }
                    }

                    renderer.Paint();
                }

                renderer.Stop();
                SDL.SDL_Quit();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Unhandled exception: {e.Message}");
                throw;
            }
        }
    }
}