using System;
using SDL2;

namespace IsometricMagic.Engine
{
    class Application
    {
        private static readonly Application Instance = new Application();
        private AppConfig _config;
        private bool _isInitialized;
        private IntPtr _sdlWindow = IntPtr.Zero;
        private IntPtr _sdlRenderer = IntPtr.Zero;
        private Renderer _renderer;
        private bool _repaintFlag;

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

            PaintWindow();
        }

        public void Stop()
        {
            if (!_isInitialized) return;

            // SpriteHolder.GetInstance().DestroyAll();
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

        public void Paint()
        {
            _renderer.DrawAll();
            PaintWindow();
            SDL.SDL_Delay(10);
        }

        public void HandleWindowEvent(SDL.SDL_Event sdlEvent)
        {
            if (sdlEvent.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_RESIZED)
            {
                _repaintFlag = true;
            }
        }

        private static void InitSdl()
        {
            var sdlInitResult = SDL.SDL_Init(SDL.SDL_INIT_VIDEO);

            if (sdlInitResult < 0)
            {
                throw new InvalidOperationException($"SDL_Init error: {SDL.SDL_GetError()}");
            }

            var sdlImageInitResult = SDL_image.IMG_Init(SDL_image.IMG_InitFlags.IMG_INIT_JPG | SDL_image.IMG_InitFlags.IMG_INIT_PNG | SDL_image.IMG_InitFlags.IMG_INIT_WEBP | SDL_image.IMG_InitFlags.IMG_INIT_TIF);

            if (sdlImageInitResult < 0)
            {
                throw new InvalidOperationException($"IMG_Init error: {SDL_image.IMG_GetError()}");
            }
        }
        
        private void InitRenderer()
        {
            _sdlRenderer = SDL.SDL_CreateRenderer(_sdlWindow, -1,
                SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED | SDL.SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC);

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

            if (_sdlWindow != IntPtr.Zero) return;
            
            Stop();
            throw new Exception($"SDL_CreateWindow error: {SDL.SDL_GetError()}");
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
        }

        public Renderer GetRenderer()
        {
            return _renderer;
        }
    }
}