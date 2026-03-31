using IsometricMagic.Engine.Diagnostics;
using IsometricMagic.Engine.Graphics;
using IsometricMagic.Engine.Graphics.SDL;
using IsometricMagic.Engine.Logging;
using NLog;
using static SDL2.SDL;

namespace IsometricMagic.Engine
{
    public sealed class ApplicationBuilder
    {
        private string _configPath = "config.ini";
        private Action<SceneManager>? _sceneConfigurator;

        public static ApplicationBuilder CreateDefault()
        {
            return new ApplicationBuilder();
        }

        public ApplicationBuilder UseConfig(string configPath)
        {
            _configPath = configPath;
            return this;
        }

        public ApplicationBuilder ConfigureScenes(Action<SceneManager> sceneConfigurator)
        {
            _sceneConfigurator = sceneConfigurator;
            return this;
        }

        public ApplicationHost Build()
        {
            return new ApplicationHost(_configPath, _sceneConfigurator);
        }
    }

    public sealed class ApplicationHost
    {
        private readonly string _configPath;
        private readonly Action<SceneManager>? _sceneConfigurator;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public ApplicationHost(string configPath, Action<SceneManager>? sceneConfigurator)
        {
            _configPath = configPath;
            _sceneConfigurator = sceneConfigurator;
        }

        public void Run()
        {
            var appConfig = new AppConfig(_configPath);
            LogBootstrap.Initialize(appConfig);
            Application? app = null;
            var sdlInitialized = false;

            try
            {
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

                var sdlOptions = SdlBootstrapOptions.CreateDefault();
                SdlBootstrap.Init(sdlOptions);
                sdlInitialized = true;

                app = Application.GetInstance();
                IGraphics graphics = appConfig.GraphicsBackend == GraphicsBackend.OpenGL
                    ? new SdlGlGraphics()
                    : new SdlGraphics();

                app.Init(appConfig, graphics);

                var sceneManager = SceneManager.GetInstance();
                _sceneConfigurator?.Invoke(sceneManager);

                var isRunning = true;
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
                            case SDL_EventType.SDL_MOUSEBUTTONDOWN:
                            case SDL_EventType.SDL_MOUSEBUTTONUP:
                            case SDL_EventType.SDL_MOUSEMOTION:
                            case SDL_EventType.SDL_CONTROLLERAXISMOTION:
                            case SDL_EventType.SDL_CONTROLLERBUTTONDOWN:
                            case SDL_EventType.SDL_CONTROLLERBUTTONUP:
                            case SDL_EventType.SDL_CONTROLLERDEVICEADDED:
                            case SDL_EventType.SDL_CONTROLLERDEVICEREMOVED:
                            case SDL_EventType.SDL_CONTROLLERDEVICEREMAPPED:
                                Input.HandleEvent(sdlEvent);
                                break;
                        }
                    }

                    app.Update();
                    app.EndTick();
                }

            }
            catch (Exception e)
            {
                Logger.Error(e, "Unhandled exception in main game loop");
                Console.Error.WriteLine($"Unhandled exception: {e.Message}");
                throw;
            }
            finally
            {
                if (app != null)
                {
                    app.Stop();
                }

                if (sdlInitialized)
                {
                    SdlBootstrap.Quit();
                }

                AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
                LogBootstrap.Shutdown();
            }
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            if (args.ExceptionObject is Exception exception)
            {
                Logger.Fatal(exception, "Unhandled exception");
                return;
            }

            Logger.Fatal("Unhandled exception object: {Object}", args.ExceptionObject);
        }
    }
}
