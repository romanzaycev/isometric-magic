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
                
                var sprite1 = new Sprite
                {
                    Texture = tex,
                    Name = "Sprite1",
                };

                var sprite2 = new Sprite
                {
                    Texture = tex,
                    Name = "Sprite2",
                };

                sprite2.Position.X = 200;
                sprite2.Position.Y = 200;

                sprite1.Sorting = 1;
                sprite2.Sorting = 2;

                while (isRunning)
                {
                    app.StartTick();
                    
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
                                    
                                    case SDL.SDL_Keycode.SDLK_SPACE:
                                        (sprite1.Sorting, sprite2.Sorting) = (sprite2.Sorting, sprite1.Sorting);
                                        break;
                                    
                                    case SDL.SDL_Keycode.SDLK_LEFT:
                                        app.GetRenderer().GetCamera().X += 10;
                                        break;
                                    
                                    case SDL.SDL_Keycode.SDLK_RIGHT:
                                        app.GetRenderer().GetCamera().X -= 10;
                                        break;
                                    
                                    case SDL.SDL_Keycode.SDLK_UP:
                                        app.GetRenderer().GetCamera().Y += 10;
                                        break;
                                    
                                    case SDL.SDL_Keycode.SDLK_DOWN:
                                        app.GetRenderer().GetCamera().Y -= 10;
                                        break;
                                }

                                break;
                        }
                    }

                    app.Paint();
                    app.EndTick();
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