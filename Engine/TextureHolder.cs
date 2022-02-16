using System.Collections.Generic;
using IsometricMagic.Engine.Graphics;

namespace IsometricMagic.Engine
{
    class TextureHolder
    {
        private static readonly TextureHolder Instance = new();
        private static readonly IGraphics Graphics = Application.GetInstance().GetGraphics();
        private readonly List<Texture> _list = new();
        private readonly Dictionary<Texture, NativeTexture> _nativeTextures = new();

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
                    var nativeTexture = Graphics.CreateTexture(
                        PixelFormat.Rgba8888,
                        (tex.TextureTarget) ? TextureAccess.Target : TextureAccess.Static,
                        tex.Width,
                        tex.Height
                    );
                    _nativeTextures.Add(tex, nativeTexture);
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

            if (_nativeTextures.ContainsKey(tex))
            {
                Graphics.DestroyTexture(_nativeTextures[tex]);
                _nativeTextures.Remove(tex);
            }
        }

        public void LoadImage(Texture tex, string imagePath)
        {
            if (!_nativeTextures.ContainsKey(tex))
            {
                PushTexture(tex);
            }

            Graphics.LoadImageToTexture(out var nTex, imagePath);
            _nativeTextures.Add(tex, nTex);
        }

        public void DestroyAll()
        {
            foreach (var tex in GetTextures())
            {
                tex.Destroy();
            }
        }

        public NativeTexture GetNativeTexture(Texture tex)
        {
            return _nativeTextures[tex];
        }
    }
}