using System;
using System.Collections.Generic;
using SDL2;

namespace IsometricMagic.Engine
{
    class TextureHolder
    {
        private static int SDL_TEXTUREACCESS_STATIC = 0;
        // private static int SDL_TEXTUREACCESS_STREAMING = 1;
        private static int SDL_TEXTUREACCESS_TARGET = 2;

        private static readonly Renderer Renderer = Application.GetInstance().GetRenderer();
        private static readonly TextureHolder Instance = new TextureHolder();
        private readonly List<Texture> _list = new List<Texture>();
        private readonly Dictionary<Texture, IntPtr> _sdlTextures = new Dictionary<Texture, IntPtr>();
        private readonly Dictionary<Texture, IntPtr> _sdlSurfaces = new Dictionary<Texture, IntPtr>();

        public static TextureHolder GetInstance()
        {
            return Instance;
        }

        public void PushTexture(Texture tex)
        {
            if (!_list.Contains(tex))
            {
                if (tex.TextureTarget)
                {
                    var sdlTexture = SDL.SDL_CreateTexture(
                        Renderer.GetSdl(),
                        SDL.SDL_PIXELFORMAT_RGBA8888,
                        (tex.TextureTarget) ? SDL_TEXTUREACCESS_TARGET : SDL_TEXTUREACCESS_STATIC,
                        tex.Width,
                        tex.Height
                    );
                    _sdlTextures.Add(tex, sdlTexture);
                }

                _list.Add(tex);
            }
        }

        public Texture[] GetTextures()
        {
            return _list.ToArray();
        }

        public void Remove(Texture tex)
        {
            _list.Remove(tex);

            if (_sdlTextures.ContainsKey(tex))
            {
                SDL.SDL_DestroyTexture(_sdlTextures[tex]);
                _sdlTextures.Remove(tex);
            }

            if (_sdlSurfaces.ContainsKey(tex))
            {
                SDL.SDL_FreeSurface(_sdlSurfaces[tex]);
                _sdlSurfaces.Remove(tex);
            }
        }

        public void LoadImage(Texture tex, AssetItem image)
        {
            if (!_sdlTextures.ContainsKey(tex))
            {
                PushTexture(tex);
            }

            var sdlSurface = SDL_image.IMG_Load(image.Path);
            var sdlTexture = SDL.SDL_CreateTextureFromSurface(Renderer.GetSdl(), sdlSurface);

            _sdlTextures.Add(tex, sdlTexture);
            _sdlSurfaces.Add(tex, sdlSurface);
        }

        public void DestroyAll()
        {
            foreach (var tex in GetTextures())
            {
                tex.Destroy();
            }
        }

        public IntPtr GetSdlTexture(Texture tex)
        {
            return _sdlTextures[tex];
        }
    }
}