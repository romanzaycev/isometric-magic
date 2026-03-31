using static SDL2.SDL;
using static SDL2.SDL_image;
using static SDL2.SDL_ttf;

namespace IsometricMagic.Engine
{
    public sealed class SdlBootstrapOptions
    {
        public uint InitFlags { get; set; }
        public IMG_InitFlags ImageFlags { get; set; }
        public required string RenderScaleQuality { get; set; }

        public static SdlBootstrapOptions CreateDefault()
        {
            return new SdlBootstrapOptions
            {
                InitFlags = SDL_INIT_VIDEO | SDL_INIT_GAMECONTROLLER | SDL_INIT_JOYSTICK,
                ImageFlags = IMG_InitFlags.IMG_INIT_JPG |
                             IMG_InitFlags.IMG_INIT_PNG |
                             IMG_InitFlags.IMG_INIT_WEBP |
                             IMG_InitFlags.IMG_INIT_TIF,
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
            var sdlInitResult = SDL_Init(resolvedOptions.InitFlags);

            if (sdlInitResult < 0)
            {
                throw new InvalidOperationException($"SDL_Init error: {SDL_GetError()}");
            }

            if (resolvedOptions.ImageFlags != 0)
            {
                var sdlImageInitResult = IMG_Init(resolvedOptions.ImageFlags);

                if (sdlImageInitResult < 0)
                {
                    throw new InvalidOperationException($"IMG_Init error: {IMG_GetError()}");
                }
            }

            if (!string.IsNullOrWhiteSpace(resolvedOptions.RenderScaleQuality))
            {
                SDL_SetHint(SDL_HINT_RENDER_SCALE_QUALITY, resolvedOptions.RenderScaleQuality);
            }

            if (TTF_WasInit() == 0)
            {
                var ttfResult = TTF_Init();
                if (ttfResult < 0)
                {
                    throw new InvalidOperationException($"TTF_Init error: {SDL_GetError()}");
                }
            }

            _isInitialized = true;
        }

        public static void InitSubSystem(uint flags)
        {
            var result = SDL_InitSubSystem(flags);
            if (result < 0)
            {
                throw new InvalidOperationException($"SDL_InitSubSystem error: {SDL_GetError()}");
            }
        }

        public static void QuitSubSystem(uint flags)
        {
            SDL_QuitSubSystem(flags);
        }

        public static void Quit()
        {
            if (!_isInitialized)
            {
                return;
            }

            IMG_Quit();
            if (TTF_WasInit() != 0)
            {
                TTF_Quit();
            }
            SDL_Quit();
            _isInitialized = false;
        }
    }
}
