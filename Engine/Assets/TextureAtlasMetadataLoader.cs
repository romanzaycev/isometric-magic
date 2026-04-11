using Newtonsoft.Json;

namespace IsometricMagic.Engine.Assets
{
    public static class TextureAtlasMetadataLoader
    {
        public static TextureAtlasMetadata Load(string atlasMetadataPath)
        {
            var json = ResourceFileSystem.ReadAllText(atlasMetadataPath);
            return Parse(json);
        }

        public static TextureAtlasMetadata Parse(string json)
        {
            var metadata = JsonConvert.DeserializeObject<TextureAtlasMetadata>(json);
            if (metadata == null)
            {
                throw new InvalidOperationException("Failed to parse texture atlas metadata.");
            }

            if (metadata.Version != 1)
            {
                throw new InvalidOperationException(
                    $"Unsupported texture atlas metadata version: {metadata.Version}. Expected version 1.");
            }

            if (metadata.Size.Width <= 0 || metadata.Size.Height <= 0)
            {
                throw new InvalidOperationException("Texture atlas size must be greater than zero.");
            }

            if (metadata.Regions.Count == 0)
            {
                throw new InvalidOperationException("Texture atlas metadata must contain at least one region.");
            }

            return metadata;
        }
    }
}
