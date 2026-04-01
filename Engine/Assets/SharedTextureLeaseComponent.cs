using System.Collections.Generic;
using IsometricMagic.Engine.SceneGraph;

namespace IsometricMagic.Engine.Assets
{
    public sealed class SharedTextureLeaseComponent : Component
    {
        private readonly List<Texture> _textures = new();

        public void Add(Texture texture)
        {
            _textures.Add(texture);
        }

        protected override void OnDestroy()
        {
            foreach (var texture in _textures)
            {
                texture.Destroy();
            }
            _textures.Clear();
        }
    }
}
