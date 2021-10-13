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
                var appConfig = new AppConfig("config.ini");
                var app = Application.GetInstance();
                app.Init(appConfig);

                SDL.SDL_Event sdlEvent;
                var isRunning = true;

                var tex = new Texture(900, 900);
                tex.LoadImage(new AssetItem("./resources/data/textures/thonk.jpeg"));
                var sprite = new Sprite();
                sprite.Texture = tex;
                
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