using Newtonsoft.Json;

namespace IsometricMagic.Engine.Assets
{
    [JsonObject]
    public sealed class TextureAtlasMetadata
    {
        [JsonProperty("version")]
        public int Version { get; private set; }

        [JsonProperty("size")]
        public required TextureAtlasSizeMetadata Size { get; init; }

        [JsonProperty("layers")]
        public required TextureAtlasLayersMetadata Layers { get; init; }

        [JsonProperty("padding")]
        public int Padding { get; private set; }

        [JsonProperty("extrude")]
        public int Extrude { get; private set; }

        [JsonProperty("regions")]
        public required Dictionary<string, TextureAtlasRegionMetadata> Regions { get; init; }
    }

    [JsonObject]
    public sealed class TextureAtlasSizeMetadata
    {
        [JsonProperty("w")]
        public int Width { get; private set; }

        [JsonProperty("h")]
        public int Height { get; private set; }
    }

    [JsonObject]
    public sealed class TextureAtlasLayersMetadata
    {
        [JsonProperty("albedo")]
        public required string Albedo { get; init; }

        [JsonProperty("normal")]
        public string? Normal { get; init; }

        [JsonProperty("emissive")]
        public string? Emissive { get; init; }
    }

    [JsonObject]
    public sealed class TextureAtlasRegionMetadata
    {
        [JsonProperty("x")]
        public int X { get; private set; }

        [JsonProperty("y")]
        public int Y { get; private set; }

        [JsonProperty("w")]
        public int Width { get; private set; }

        [JsonProperty("h")]
        public int Height { get; private set; }
    }
}
