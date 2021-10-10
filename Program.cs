using System;
using SDL2;

namespace IsometricMagic
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var sdlInitResult = SDL.SDL_Init(SDL.SDL_INIT_VIDEO);

                if (sdlInitResult < 0)
                {
                    throw new InvalidOperationException($"SDL_Init error: {SDL.SDL_GetError()}");
                }

                try
                {
                    var window = SDL.SDL_CreateWindow(
                        "Isometric Magic",
                        SDL.SDL_WINDOWPOS_CENTERED,
                        SDL.SDL_WINDOWPOS_CENTERED,
                        1366,
                        768,
                        SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE
                    );

                    if (window == IntPtr.Zero)
                    {
                        throw new Exception($"SDL_CreateWindow error: {SDL.SDL_GetError()}");
                    }

                    bool repaint = true;
                    Action paintWindow = () =>
                    {
                        if (!repaint)
                        {
                            return;
                        }

                        repaint = false;

                        IntPtr windowSurface = SDL.SDL_GetWindowSurface(window);
                        int w, h;
                        SDL.SDL_GetWindowSize(window, out w, out h);

                        SDL.SDL_Rect windowRect;
                        windowRect.x = 0;
                        windowRect.y = 0;
                        windowRect.w = w;
                        windowRect.h = h;

                        SDL.SDL_FillRect(windowSurface, ref windowRect, 0xff000000);
                        SDL.SDL_UpdateWindowSurface(window);
                    };

                    paintWindow();

                    SDL.SDL_Event sdlEvent;
                    bool isRunning = true;

                    while (isRunning)
                    {
                        while (SDL.SDL_PollEvent(out sdlEvent) != 0)
                        {
                            switch (sdlEvent.type)
                            {
                                case SDL.SDL_EventType.SDL_WINDOWEVENT:
                                    if (sdlEvent.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_RESIZED)
                                    {
                                        repaint = true;
                                    }

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

                        paintWindow();
                    }

                    SDL.SDL_DestroyWindow(window);
                }
                finally
                {
                    SDL.SDL_Quit();
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Unhandled exception: {e.Message}");
                throw;
            }
        }
    }
}