using System.Collections.Generic;

namespace IsometricMagic.Engine
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
            var holder = TextureHolder.GetInstance();
            foreach (var texture in _textures)
            {
                holder.ReleaseSharedTexture(texture);
            }
            _textures.Clear();
        }
    }
}
