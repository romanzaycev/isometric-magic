using IsometricMagic.Engine;
using IsometricMagic.Game.Scenes;
using static SDL2.SDL;

namespace IsometricMagic
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var appConfig = new AppConfig("config.ini");
                var sdlOptions = SdlBootstrapOptions.CreateDefault();
                SdlBootstrap.Init(sdlOptions);
                var app = Application.GetInstance();
                var graphics = new Engine.Graphics.SDL.SdlGraphics();
                
                app.Init(appConfig, graphics);

                var isRunning = true;
                var sceneManager = SceneManager.GetInstance();
                
                sceneManager.SetLoadingScene(new Loading());

                sceneManager.Add(new IsoTest());
                //sceneManager.Add(new Main());
                //sceneManager.Add(new Animation());
                //sceneManager.Add(new Second());
                //sceneManager.Add(new Bench());

                while (isRunning)
                {
                    app.StartTick();
                    
                    Input.BeginFrame();
                    
                    SDL_Event sdlEvent;
                    
                    while (SDL_PollEvent(out sdlEvent) != 0)
                    {
                        switch (sdlEvent.type)
                        {
                            case SDL_EventType.SDL_WINDOWEVENT:
                                app.HandleWindowEvent(sdlEvent);
                                break;

                            case SDL_EventType.SDL_QUIT:
                                isRunning = false;
                                break;

                            case SDL_EventType.SDL_KEYDOWN:
                                if (sdlEvent.key.keysym.sym == SDL_Keycode.SDLK_ESCAPE)
                                {
                                    isRunning = false;
                                }
                                Input.HandleEvent(sdlEvent);
                                break;
                             
                            case SDL_EventType.SDL_KEYUP:
                                Input.HandleEvent(sdlEvent);
                                break;
                             
                            case SDL_EventType.SDL_MOUSEBUTTONDOWN:
                                Input.HandleEvent(sdlEvent);
                                break;
                                 
                            case SDL_EventType.SDL_MOUSEBUTTONUP:
                                Input.HandleEvent(sdlEvent);
                                break;
                             
                            case SDL_EventType.SDL_MOUSEMOTION:
                                Input.HandleEvent(sdlEvent);
                                break;
                            case SDL_EventType.SDL_CONTROLLERAXISMOTION:
                                Input.HandleEvent(sdlEvent);
                                break;
                            case SDL_EventType.SDL_CONTROLLERBUTTONDOWN:
                                Input.HandleEvent(sdlEvent);
                                break;
                            case SDL_EventType.SDL_CONTROLLERBUTTONUP:
                                Input.HandleEvent(sdlEvent);
                                break;
                            case SDL_EventType.SDL_CONTROLLERDEVICEADDED:
                                Input.HandleEvent(sdlEvent);
                                break;
                            case SDL_EventType.SDL_CONTROLLERDEVICEREMOVED:
                                Input.HandleEvent(sdlEvent);
                                break;
                            case SDL_EventType.SDL_CONTROLLERDEVICEREMAPPED:
                                Input.HandleEvent(sdlEvent);
                                break;
                        }
                    }

                    app.Update();
                    app.EndTick();
                }

                app.Stop();
                SdlBootstrap.Quit();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Unhandled exception: {e.Message}");
                throw;
            }
        }
    }
}
