namespace IsometricMagic.Engine.Tests;

public sealed class TextureAtlasMetadataLoaderTests
{
    [Fact]
    public void Parse_ValidMetadata_ParsesLayersAndRegions()
    {
        const string json = """
                            {
                              "version": 1,
                              "size": { "w": 1024, "h": 1024 },
                              "layers": {
                                "albedo": "dungeon.png",
                                "normal": "dungeon_normal.png",
                                "emissive": "dungeon_emissive.png"
                              },
                              "padding": 2,
                              "extrude": 1,
                              "regions": {
                                "ts1/dirt_W.png": { "x": 0, "y": 0, "w": 256, "h": 512 }
                              }
                            }
                            """;

        var metadata = TextureAtlasMetadataLoader.Parse(json);

        Assert.Equal(1, metadata.Version);
        Assert.Equal(1024, metadata.Size.Width);
        Assert.Equal(1024, metadata.Size.Height);
        Assert.Equal("dungeon.png", metadata.Layers.Albedo);
        Assert.Equal("dungeon_normal.png", metadata.Layers.Normal);
        Assert.Equal("dungeon_emissive.png", metadata.Layers.Emissive);
        Assert.Equal(2, metadata.Padding);
        Assert.Equal(1, metadata.Extrude);
        Assert.True(metadata.Regions.ContainsKey("ts1/dirt_W.png"));
    }

    [Fact]
    public void Parse_UnsupportedVersion_Throws()
    {
        const string json = """
                            {
                              "version": 2,
                              "size": { "w": 16, "h": 16 },
                              "layers": { "albedo": "a.png" },
                              "regions": {
                                "a.png": { "x": 0, "y": 0, "w": 16, "h": 16 }
                              }
                            }
                            """;

        Assert.Throws<InvalidOperationException>(() => TextureAtlasMetadataLoader.Parse(json));
    }
}
