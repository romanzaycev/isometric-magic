using System;
using SDL2;
using IsometricMagic.Engine;

namespace IsometricMagic
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            try
            {
                AppConfig appConfig = new AppConfig("config.ini");
                Application app = Application.GetInstance();
                app.Init(appConfig);

                SDL.SDL_Event sdlEvent;
                bool isRunning = true;

                while (isRunning)
                {
                    while (SDL.SDL_PollEvent(out sdlEvent) != 0)
                    {
                        switch (sdlEvent.type)
                        {
                            case SDL.SDL_EventType.SDL_WINDOWEVENT:
                                app.HandleWindowEvent(sdlEvent);
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

                    app.Paint();
                }

                app.Stop();
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