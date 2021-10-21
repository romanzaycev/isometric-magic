using System;
using SDL2;

namespace IsometricMagic.Engine
{
    class Application
    {
        private static readonly Application Instance = new();
        private static readonly SceneManager SceneManager = SceneManager.GetInstance();
        private AppConfig _config;
        private bool _isInitialized;
        private IntPtr _sdlWindow = IntPtr.Zero;
        private IntPtr _sdlRenderer = IntPtr.Zero;
        private Renderer _renderer;
        private bool _repaintFlag;
        private ulong _desiredDelta;
        private ulong _startTick;

        private static ulong DT_LAST;
        private static float _deltaTime;
        public static float DeltaTime => _deltaTime;
        
        public static Application GetInstance()
        {
            return Instance;
        }

        public void Init(AppConfig config)
        {
            if (_isInitialized)
            {
                return;
            }

            _config = config;

            InitSdl();
            InitWindow();
            InitRenderer();

            _repaintFlag = true;
            _isInitialized = true;

            if (_config.TargetFps > 0 && !_config.VSync)
            {
                _desiredDelta = 1000 / (ulong) _config.TargetFps;
            }

            DT_LAST = 0;

            PaintWindow();
        }

        public void Stop()
        {
            if (!_isInitialized) return;

            SceneManager.GetCurrent().Unload();
            TextureHolder.GetInstance().DestroyAll();

            if (_sdlRenderer != IntPtr.Zero)
            {
                SDL.SDL_DestroyRenderer(_sdlRenderer);
            }

            if (_sdlWindow != IntPtr.Zero)
            {
                SDL.SDL_DestroyWindow(_sdlWindow);
            }
        }

        public void Update()
        {
            var now = SDL.SDL_GetTicks();

            if (now > DT_LAST)
            {
                _deltaTime =  (float)(now - DT_LAST) / 1000;
                DT_LAST = now;
            }
            
            SceneManager.GetCurrent().Update();
            _renderer.DrawAll();
            PaintWindow();
        }

        public void HandleWindowEvent(SDL.SDL_Event sdlEvent)
        {
            if (sdlEvent.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_RESIZED)
            {
                _repaintFlag = true;
            }
        }

        public Renderer GetRenderer()
        {
            return _renderer;
        }

        public void StartTick()
        {
            _startTick = SDL.SDL_GetPerformanceCounter();
        }

        public void EndTick()
        {
            var end = SDL.SDL_GetPerformanceCounter();
            var freq = SDL.SDL_GetPerformanceFrequency();

            if (_desiredDelta > 0 && freq > 0)
            {
                var delta = end - _startTick;
                var elapsedMs = delta / freq * 1000;

                if (_desiredDelta > elapsedMs)
                {
                    SDL.SDL_Delay((uint) Math.Floor((float) (_desiredDelta - elapsedMs)));
                }
            }
        }

        public AppConfig GetConfig()
        {
            return _config;
        }

        private static void InitSdl()
        {
            var sdlInitResult = SDL.SDL_Init(SDL.SDL_INIT_VIDEO);

            if (sdlInitResult < 0)
            {
                throw new InvalidOperationException($"SDL_Init error: {SDL.SDL_GetError()}");
            }

            var sdlImageInitResult = SDL_image.IMG_Init(SDL_image.IMG_InitFlags.IMG_INIT_JPG |
                                                        SDL_image.IMG_InitFlags.IMG_INIT_PNG |
                                                        SDL_image.IMG_InitFlags.IMG_INIT_WEBP |
                                                        SDL_image.IMG_InitFlags.IMG_INIT_TIF);

            if (sdlImageInitResult < 0)
            {
                throw new InvalidOperationException($"IMG_Init error: {SDL_image.IMG_GetError()}");
            }
        }

        private void InitRenderer()
        {
            var flags = SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED;

            if (_config.VSync) flags |= SDL.SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC;

            _sdlRenderer = SDL.SDL_CreateRenderer(
                _sdlWindow,
                -1,
                flags
            );

            if (_sdlRenderer == IntPtr.Zero)
            {
                Stop();
                throw new InvalidOperationException($"SDL_CreateRenderer error: {SDL.SDL_GetError()}");
            }

            _renderer = new Renderer(_sdlRenderer);
        }

        private void InitWindow()
        {
            _sdlWindow = SDL.SDL_CreateWindow(
                "Isometric Magic",
                SDL.SDL_WINDOWPOS_CENTERED,
                SDL.SDL_WINDOWPOS_CENTERED,
                _config.WindowWidth,
                _config.WindowHeight,
                SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE
            );

            if (_sdlWindow == IntPtr.Zero)
            {
                Stop();
                throw new Exception($"SDL_CreateWindow error: {SDL.SDL_GetError()}");
            }

            if (_config.IsFullscreen)
            {
                SDL.SDL_SetWindowFullscreen(_sdlWindow, (uint) SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN);
            }
        }

        private void PaintWindow()
        {
            if (!_repaintFlag)
            {
                return;
            }

            _repaintFlag = false;

            var windowSurface = SDL.SDL_GetWindowSurface(_sdlWindow);
            SDL.SDL_GetWindowSize(_sdlWindow, out var w, out var h);

            SDL.SDL_Rect windowRect;
            windowRect.x = 0;
            windowRect.y = 0;
            windowRect.w = w;
            windowRect.h = h;

            SDL.SDL_FillRect(windowSurface, ref windowRect, 0xff000000);
            SDL.SDL_UpdateWindowSurface(_sdlWindow);
            
            _renderer.HandleWindowResized(w, h);
        }
    }
}