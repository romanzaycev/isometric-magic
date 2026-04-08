namespace IsometricMagic.Engine.Tests;

public sealed class TextureUvMapperTests
{
    [Fact]
    public void Resolve_WithoutRegion_ReturnsFullUvRange()
    {
        var uv = TextureUvMapper.Resolve(null, 1024, 512);

        Assert.Equal(0f, uv.MinX);
        Assert.Equal(0f, uv.MinY);
        Assert.Equal(1f, uv.MaxX);
        Assert.Equal(1f, uv.MaxY);
    }

    [Fact]
    public void Resolve_WithRegion_ReturnsNormalizedBounds()
    {
        var region = new TextureRegion(256, 128, 512, 256);

        var uv = TextureUvMapper.Resolve(region, 2048, 1024);

        Assert.Equal(0.125f, uv.MinX);
        Assert.Equal(0.125f, uv.MinY);
        Assert.Equal(0.375f, uv.MaxX);
        Assert.Equal(0.375f, uv.MaxY);
    }

    [Fact]
    public void Expand_AddsPaddingOnAllSides()
    {
        var initial = new TextureUvBounds(0.2f, 0.3f, 0.7f, 0.9f);

        var expanded = TextureUvMapper.Expand(initial, 0.05f, 0.1f);

        Assert.Equal(0.15f, expanded.MinX, 5);
        Assert.Equal(0.2f, expanded.MinY, 5);
        Assert.Equal(0.75f, expanded.MaxX, 5);
        Assert.Equal(1.0f, expanded.MaxY, 5);
    }
}
