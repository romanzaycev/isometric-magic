using System;
using SDL2;

namespace IsometricMagic
{
    public class Renderer
    {
        private static readonly Renderer Instance = new Renderer();
        
        private bool _isInitialized;
        private IntPtr _window = IntPtr.Zero;
        private bool _repaint;

        public static Renderer GetInstance()
        {
            return Instance;
        }

        public void Init()
        {
            if (_isInitialized)
            {
                return;
            }
            
            var sdlInitResult = SDL.SDL_Init(SDL.SDL_INIT_VIDEO);

            if (sdlInitResult < 0)
            {
                throw new InvalidOperationException($"SDL_Init error: {SDL.SDL_GetError()}");
            }
            
            _window = SDL.SDL_CreateWindow(
                "Isometric Magic",
                SDL.SDL_WINDOWPOS_CENTERED,
                SDL.SDL_WINDOWPOS_CENTERED,
                1366,
                768,
                SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE
            );

            if (_window == IntPtr.Zero)
            {
                Stop();
                throw new Exception($"SDL_CreateWindow error: {SDL.SDL_GetError()}");
            }

            _repaint = true;
            _isInitialized = true;
            
            PaintWindow();
        }

        public void Stop()
        {
            if (_isInitialized)
            {
                if (_window != IntPtr.Zero)
                {
                    SDL.SDL_DestroyWindow(_window);
                }
            }
            
            SDL.SDL_Quit();
        }

        public void Paint()
        {
            PaintWindow();
        }

        private void PaintWindow()
        {
            if (!_repaint)
            {
                return;
            }

            _repaint = false;

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

        public void HandleWindowEvent(SDL.SDL_Event sdlEvent)
        {
            if (sdlEvent.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_RESIZED)
            {
                _repaint = true;
            }
        }
    }
}