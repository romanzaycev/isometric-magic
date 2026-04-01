using System.Collections.Generic;
using IsometricMagic.Engine.App;
using IsometricMagic.Engine.Assets;
using IsometricMagic.Engine.Core.Graphics;
using IsometricMagic.Engine.Graphics.OpenGL;

namespace IsometricMagic.Engine.Core.Assets
{
    internal class TextureHolder
    {
        private static readonly TextureHolder Instance = new();
        private static readonly IGraphics Graphics = Application.GetInstance().GetGraphics();
        private readonly List<Texture> _list = new();
        private readonly Dictionary<Texture, NativeTexture> _nativeTextures = new();
        private readonly Dictionary<Texture, int> _refCounts = new();
        private readonly Dictionary<string, Texture> _sharedTexturesByPath = new();
        private readonly Dictionary<Texture, string> _sharedTexturePaths = new();

        public static TextureHolder GetInstance()
        {
            return Instance;
        }

        public void PushTexture(Texture tex)
        {
            if (_refCounts.ContainsKey(tex))
            {
                return;
            }

            if (tex.TextureTarget)
            {
                var nativeTexture = Graphics.CreateTexture(
                    PixelFormat.Rgba8888,
                    tex.TextureTarget ? TextureAccess.Target : TextureAccess.Static,
                    tex.Width,
                    tex.Height
                );
                _nativeTextures.Add(tex, nativeTexture);
            }

            _list.Add(tex);
            _refCounts[tex] = 1;
        }

        public void AddReference(Texture tex)
        {
            if (!_refCounts.TryGetValue(tex, out var count))
            {
                PushTexture(tex);
                return;
            }

            _refCounts[tex] = count + 1;
        }

        public Texture[] GetTextures()
        {
            return _list.ToArray();
        }

        public void Remove(Texture tex)
        {
            if (!_refCounts.TryGetValue(tex, out var count))
            {
                return;
            }

            var nextCount = count - 1;
            if (nextCount > 0)
            {
                _refCounts[tex] = nextCount;
                return;
            }

            _refCounts.Remove(tex);
            _list.Remove(tex);

            if (_nativeTextures.ContainsKey(tex))
            {
                Graphics.DestroyTexture(_nativeTextures[tex]);
                _nativeTextures.Remove(tex);
            }

            if (_sharedTexturePaths.TryGetValue(tex, out var path))
            {
                _sharedTexturePaths.Remove(tex);
                _sharedTexturesByPath.Remove(path);
            }
        }

        public Texture AcquireSharedTexture(string imagePath, int width, int height)
        {
            if (_sharedTexturesByPath.TryGetValue(imagePath, out var cached))
            {
                AddReference(cached);
                return cached;
            }

            var tex = new Texture(width, height);
            tex.LoadImage(imagePath);
            _sharedTexturesByPath[imagePath] = tex;
            _sharedTexturePaths[tex] = imagePath;
            return tex;
        }

        public void ReleaseSharedTexture(Texture texture)
        {
            Remove(texture);
        }

        public void LoadImage(Texture tex, string imagePath)
        {
            if (!_nativeTextures.ContainsKey(tex))
            {
                PushTexture(tex);
            }

            Graphics.LoadImageToTexture(out var nTex, imagePath);
            if (_nativeTextures.ContainsKey(tex))
            {
                Graphics.DestroyTexture(_nativeTextures[tex]);
                _nativeTextures[tex] = nTex;
            }
            else
            {
                _nativeTextures.Add(tex, nTex);
            }
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
