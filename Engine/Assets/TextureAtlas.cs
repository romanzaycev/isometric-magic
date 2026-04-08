namespace IsometricMagic.Engine.Assets
{
    public sealed class TextureAtlas
    {
        private readonly Dictionary<string, TextureRegion> _regions;

        public Texture AlbedoTexture { get; }
        public Texture? NormalTexture { get; }
        public Texture? EmissiveTexture { get; }
        public IReadOnlyDictionary<string, TextureRegion> Regions => _regions;

        public TextureAtlas(
            Texture albedoTexture,
            Texture? normalTexture,
            Texture? emissiveTexture,
            IReadOnlyDictionary<string, TextureRegion> regions)
        {
            AlbedoTexture = albedoTexture;
            NormalTexture = normalTexture;
            EmissiveTexture = emissiveTexture;
            _regions = new Dictionary<string, TextureRegion>(regions, StringComparer.Ordinal);
        }

        public bool TryGetRegion(string regionKey, out TextureRegion region)
        {
            return _regions.TryGetValue(regionKey, out region);
        }

        public void Destroy()
        {
            AlbedoTexture.Destroy();

            if (NormalTexture != null && !ReferenceEquals(NormalTexture, AlbedoTexture))
            {
                NormalTexture.Destroy();
            }

            if (EmissiveTexture != null
                && !ReferenceEquals(EmissiveTexture, AlbedoTexture)
                && !ReferenceEquals(EmissiveTexture, NormalTexture))
            {
                EmissiveTexture.Destroy();
            }
        }
    }
}
