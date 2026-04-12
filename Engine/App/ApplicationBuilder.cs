using IonMotion.Engine.Diagnostics;
using IonMotion.Engine.Core.Graphics;
using IonMotion.Engine.Core.Graphics.SDL;
using IonMotion.Engine.Core.Platform.Sdl;
using IonMotion.Engine.Core.Logging;
using IonMotion.Engine.Scenes;
using IonMotion.Engine.Inputs;
using NLog;
using System.Collections.Generic;
using System.Threading.Tasks;
using static SDL2.SDL;

namespace IonMotion.Engine.App
{
    public sealed class ApplicationBuilder
    {
        private string _configPath = "config.ini";
        private string _appName = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name ?? "IonMotion";
        private Action<SceneManager>? _sceneConfigurator;
        private Action<List<IApplicationRuntimeService>>? _runtimeServicesConfigurator;

        public static ApplicationBuilder CreateDefault()
        {
            return new ApplicationBuilder();
        }

        public ApplicationBuilder UseConfig(string configPath)
        {
            _configPath = configPath;
            return this;
        }

        public ApplicationBuilder UseAppName(string appName)
        {
            if (string.IsNullOrWhiteSpace(appName))
            {
                throw new ArgumentException("Application name cannot be null or whitespace.", nameof(appName));
            }

            _appName = appName;
            return this;
        }

        public ApplicationBuilder ConfigureScenes(Action<SceneManager> sceneConfigurator)
        {
            _sceneConfigurator = sceneConfigurator;
            return this;
        }

        public ApplicationBuilder ConfigureRuntimeServices(Action<List<IApplicationRuntimeService>> runtimeServicesConfigurator)
        {
            _runtimeServicesConfigurator = runtimeServicesConfigurator;
            return this;
        }

        public ApplicationHost Build()
        {
            return new ApplicationHost(_configPath, _appName, _sceneConfigurator, _runtimeServicesConfigurator);
        }
    }

    public sealed class ApplicationHost
    {
        private readonly string _configPath;
        private readonly string _appName;
        private readonly Action<SceneManager>? _sceneConfigurator;
        private readonly Action<List<IApplicationRuntimeService>>? _runtimeServicesConfigurator;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly FrameStats FrameStats = FrameStats.GetInstance();

        public ApplicationHost(
            string configPath,
            string appName,
            Action<SceneManager>? sceneConfigurator,
            Action<List<IApplicationRuntimeService>>? runtimeServicesConfigurator)
        {
            _configPath = configPath;
            _appName = appName;
            _sceneConfigurator = sceneConfigurator;
            _runtimeServicesConfigurator = runtimeServicesConfigurator;
        }

        public void Run()
        {
            Application? app = null;
            var sdlInitialized = false;
            var hasFatalError = false;
            var fatalExitCode = 0;

            try
            {
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
                TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

                var appConfig = new AppConfig(_configPath);
                LogBootstrap.Initialize(appConfig);

                var sdlOptions = SdlBootstrapOptions.CreateDefault();
                SdlBootstrap.Init(sdlOptions);
                sdlInitialized = true;

                app = Application.GetInstance();
                IGraphics graphics = new SdlGlGraphics();

                var runtimeServices = new List<IApplicationRuntimeService>();
                _runtimeServicesConfigurator?.Invoke(runtimeServices);
                app.SetRuntimeServices(runtimeServices);

                app.Init(appConfig, graphics, _appName);

                var sceneManager = SceneManager.GetInstance();
                _sceneConfigurator?.Invoke(sceneManager);

                var isRunning = true;
                while (isRunning)
                {
                    app.StartTick();

                    Input.BeginFrame();
                    var eventLoopStart = SDL_GetPerformanceCounter();

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

                    var eventLoopEnd = SDL_GetPerformanceCounter();
                    var eventLoopFrequency = SDL_GetPerformanceFrequency();
                    if (eventLoopFrequency > 0 && eventLoopEnd >= eventLoopStart)
                    {
                        var eventLoopMs = (float) ((double) (eventLoopEnd - eventLoopStart) * 1000.0 / eventLoopFrequency);
                        FrameStats.SetEventLoopMs(eventLoopMs);
                    }

                    app.Update();
                    app.EndTick();
                }

            }
            catch (Exception e)
            {
                Logger.Fatal(e, "Fatal exception in main game loop");
                TryFlushLogs();
                fatalExitCode = FatalErrorReporter.Report(e, LogBootstrap.CurrentErrorLogPath);
                hasFatalError = true;
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
                TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
                LogBootstrap.Shutdown();
            }

            if (hasFatalError)
            {
                Environment.ExitCode = fatalExitCode;
            }
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            if (args.ExceptionObject is Exception exception)
            {
                Logger.Fatal(exception, "Unhandled exception");
                TryFlushLogs();
                Environment.ExitCode = FatalErrorReporter.Report(exception, LogBootstrap.CurrentErrorLogPath);
                return;
            }

            Logger.Fatal("Unhandled exception object: {Object}", args.ExceptionObject);
            TryFlushLogs();
            Environment.ExitCode = FatalErrorReporter.ReportNonException(args.ExceptionObject, LogBootstrap.CurrentErrorLogPath);
        }

        private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
        {
            Logger.Error(args.Exception, "Unobserved task exception");
            args.SetObserved();
        }

        private static void TryFlushLogs()
        {
            try
            {
                LogManager.Flush(TimeSpan.FromSeconds(2));
            }
            catch
            {
            }
        }
    }
}
