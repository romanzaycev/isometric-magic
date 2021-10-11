using System;
using SDL2;

namespace IsometricMagic.Engine
{
    public class Application
    {
        private static readonly Application Instance = new Application();
        private AppConfig _config;
        private bool _isInitialized;
        private IntPtr _window = IntPtr.Zero;
        private IntPtr _renderer = IntPtr.Zero;
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
            
            if (_renderer != IntPtr.Zero)
            {
                SDL.SDL_DestroyRenderer(_renderer);
            }

            if (_window != IntPtr.Zero)
            {
                SDL.SDL_DestroyWindow(_window);
            }
        }

        public void Paint()
        {
            SDL.SDL_SetRenderDrawColor(_renderer,0x00, 0x00, 0x00, 0x00);
            SDL.SDL_RenderClear(_renderer);
            
            // Draw all scripts
            
            SDL.SDL_RenderPresent(_renderer);
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
        }
        
        private void InitRenderer()
        {
            _renderer = SDL.SDL_CreateRenderer(_window, -1,
                SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED | SDL.SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC);

            if (_renderer == IntPtr.Zero)
            {
                Stop();
                throw new InvalidOperationException($"SDL_CreateRenderer error: {SDL.SDL_GetError()}");
            }
        }

        private void InitWindow()
        {
            _window = SDL.SDL_CreateWindow(
                "Isometric Magic",
                SDL.SDL_WINDOWPOS_CENTERED,
                SDL.SDL_WINDOWPOS_CENTERED,
                _config.WindowWidth,
                _config.WindowHeight,
                SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE
            );

            if (_window == IntPtr.Zero)
            {
                Stop();
                throw new Exception($"SDL_CreateWindow error: {SDL.SDL_GetError()}");
            }
        }

        private void PaintWindow()
        {
            if (!_repaintFlag)
            {
                return;
            }

            _repaintFlag = false;

            IntPtr windowSurface = SDL.SDL_GetWindowSurface(_window);
            SDL.SDL_GetWindowSize(_window, out var w, out var h);

            SDL.SDL_Rect windowRect;
            windowRect.x = 0;
            windowRect.y = 0;
            windowRect.w = w;
            windowRect.h = h;

            SDL.SDL_FillRect(windowSurface, ref windowRect, 0xff000000);
            SDL.SDL_UpdateWindowSurface(_window);
        }
    }
}