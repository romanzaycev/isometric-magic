using System;
using SDL2;
using IsometricMagic.Engine;
using IsometricMagic.Game.Scenes;

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

                SceneManager.GetInstance().Add(new Main());
                SceneManager.GetInstance().Add(new Second());
                SceneManager.GetInstance().Add(new Bench());

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
                                }

                                Input.HandleKeyboardEvent(sdlEvent);
                                break;
                            
                            case SDL.SDL_EventType.SDL_KEYUP:
                                Input.HandleKeyboardEvent(sdlEvent);
                                break;
                            
                            case SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN:
                                Input.HandleMouseBtnEvent(sdlEvent);
                                break;
                                
                            case SDL.SDL_EventType.SDL_MOUSEBUTTONUP:
                                Input.HandleMouseBtnEvent(sdlEvent);
                                break;
                            
                            case SDL.SDL_EventType.SDL_MOUSEMOTION:
                                Input.HandleMousePos(sdlEvent.motion.x, sdlEvent.motion.y);
                                break;
                        }
                    }

                    app.Update();
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