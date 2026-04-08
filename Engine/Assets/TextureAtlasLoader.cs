namespace IsometricMagic.Engine.Assets
{
    public static class TextureAtlasLoader
    {
        public static TextureAtlas Load(string atlasMetadataPath)
        {
            var metadata = TextureAtlasMetadataLoader.Load(atlasMetadataPath);
            var baseDirectory = Path.GetDirectoryName(Path.GetFullPath(atlasMetadataPath))
                ?? throw new InvalidOperationException("Failed to resolve texture atlas metadata directory.");

            var atlasWidth = metadata.Size.Width;
            var atlasHeight = metadata.Size.Height;
            var albedoPath = ResolveLayerPath(baseDirectory, metadata.Layers.Albedo);
            var albedoTexture = Texture.AcquireShared(albedoPath, atlasWidth, atlasHeight);

            Texture? normalTexture = null;
            if (!string.IsNullOrWhiteSpace(metadata.Layers.Normal))
            {
                var normalPath = ResolveLayerPath(baseDirectory, metadata.Layers.Normal);
                normalTexture = Texture.AcquireShared(normalPath, atlasWidth, atlasHeight);
            }

            Texture? emissiveTexture = null;
            if (!string.IsNullOrWhiteSpace(metadata.Layers.Emissive))
            {
                var emissivePath = ResolveLayerPath(baseDirectory, metadata.Layers.Emissive);
                emissiveTexture = Texture.AcquireShared(emissivePath, atlasWidth, atlasHeight);
            }

            var regions = new Dictionary<string, TextureRegion>(metadata.Regions.Count, StringComparer.Ordinal);
            foreach (var pair in metadata.Regions)
            {
                var region = pair.Value;
                regions[pair.Key] = new TextureRegion(region.X, region.Y, region.Width, region.Height);
            }

            return new TextureAtlas(albedoTexture, normalTexture, emissiveTexture, regions);
        }

        private static string ResolveLayerPath(string baseDirectory, string layerPath)
        {
            if (Path.IsPathRooted(layerPath))
            {
                return layerPath;
            }

            return Path.GetFullPath(Path.Combine(baseDirectory, layerPath));
        }
    }
}
