using System;
using SDL2;

namespace IsometricMagic.Engine
{
    public sealed class SdlBootstrapOptions
    {
        public uint InitFlags { get; set; }
        public SDL_image.IMG_InitFlags ImageFlags { get; set; }
        public string RenderScaleQuality { get; set; }

        public static SdlBootstrapOptions CreateDefault()
        {
            return new SdlBootstrapOptions
            {
                InitFlags = SDL.SDL_INIT_VIDEO | SDL.SDL_INIT_GAMECONTROLLER | SDL.SDL_INIT_JOYSTICK,
                ImageFlags = SDL_image.IMG_InitFlags.IMG_INIT_JPG |
                             SDL_image.IMG_InitFlags.IMG_INIT_PNG |
                             SDL_image.IMG_InitFlags.IMG_INIT_WEBP |
                             SDL_image.IMG_InitFlags.IMG_INIT_TIF,
                RenderScaleQuality = "best"
            };
        }
    }

    public static class SdlBootstrap
    {
        private static bool _isInitialized;

        public static bool IsInitialized => _isInitialized;

        public static void Init(SdlBootstrapOptions options)
        {
            if (_isInitialized)
            {
                return;
            }

            var resolvedOptions = options ?? SdlBootstrapOptions.CreateDefault();
            var sdlInitResult = SDL.SDL_Init(resolvedOptions.InitFlags);

            if (sdlInitResult < 0)
            {
                throw new InvalidOperationException($"SDL_Init error: {SDL.SDL_GetError()}");
            }

            if (resolvedOptions.ImageFlags != 0)
            {
                var sdlImageInitResult = SDL_image.IMG_Init(resolvedOptions.ImageFlags);

                if (sdlImageInitResult < 0)
                {
                    throw new InvalidOperationException($"IMG_Init error: {SDL_image.IMG_GetError()}");
                }
            }

            if (!string.IsNullOrWhiteSpace(resolvedOptions.RenderScaleQuality))
            {
                SDL.SDL_SetHint(SDL.SDL_HINT_RENDER_SCALE_QUALITY, resolvedOptions.RenderScaleQuality);
            }

            _isInitialized = true;
        }

        public static void InitSubSystem(uint flags)
        {
            var result = SDL.SDL_InitSubSystem(flags);
            if (result < 0)
            {
                throw new InvalidOperationException($"SDL_InitSubSystem error: {SDL.SDL_GetError()}");
            }
        }

        public static void QuitSubSystem(uint flags)
        {
            SDL.SDL_QuitSubSystem(flags);
        }

        public static void Quit()
        {
            if (!_isInitialized)
            {
                return;
            }

            SDL_image.IMG_Quit();
            SDL.SDL_Quit();
            _isInitialized = false;
        }
    }
}
