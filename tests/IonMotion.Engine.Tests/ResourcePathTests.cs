using IonMotion.Engine.Assets;
using Xunit;

namespace IonMotion.Engine.Tests;

public sealed class ResourcePathTests
{
    [Fact]
    public void ResolveFromFile_ResolvesSiblingFileInSameDirectory()
    {
        var resolved = ResourcePath.ResolveFromFile("resources/_gen/atlases/ts1.json", "ts1.png");
        Assert.Equal("resources/_gen/atlases/ts1.png", resolved);
    }

    [Fact]
    public void ResolveFromDirectory_ResolvesRelativePathFromDirectory()
    {
        var resolved = ResourcePath.ResolveFromDirectory("resources/_gen/atlases", "ts1.png");
        Assert.Equal("resources/_gen/atlases/ts1.png", resolved);
    }

    [Fact]
    public void ResolveFromFile_ResourceAbsoluteReference_PreservesAbsoluteResourcePath()
    {
        var resolved = ResourcePath.ResolveFromFile("resources/_gen/atlases/ts1.json", "resources/data/maps/map1.json");
        Assert.Equal("resources/data/maps/map1.json", resolved);
    }

    [Fact]
    public void ResolveFromFile_DotSlashResourcesReference_PreservesAbsoluteResourcePath()
    {
        var resolved = ResourcePath.ResolveFromFile("resources/_gen/atlases/ts1.json", "./resources/data/maps/map1.json");
        Assert.Equal("resources/data/maps/map1.json", resolved);
    }

    [Fact]
    public void NormalizeResourcePath_AbsolutePathOutsideResources_Throws()
    {
        var external = Path.Combine(Path.GetTempPath(), "savegame.dat");
        var error = Assert.Throws<InvalidOperationException>(() => ResourcePath.NormalizeResourcePath(external));
        Assert.Contains("outside the 'resources' entry point", error.Message);
    }

    [Fact]
    public void TryNormalizeResourcePath_AbsolutePathOutsideResources_ReturnsFalse()
    {
        var external = Path.Combine(Path.GetTempPath(), "savegame.dat");
        var ok = ResourcePath.TryNormalizeResourcePath(external, out var normalized);

        Assert.False(ok);
        Assert.Equal(string.Empty, normalized);
    }
}
