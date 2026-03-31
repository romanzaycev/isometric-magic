using System.Drawing;
using System.Numerics;
using IsometricMagic.Engine;

namespace IsometricMagic.Engine.Tests;

public sealed class CameraComposerTests
{
    [Fact]
    public void Apply_WithNoInfluences_DoesNothing()
    {
        var camera = new Camera(100, 80);
        camera.Rect = new Rectangle(10, 20, 100, 80);
        camera.SubpixelOffset = new Vector2(0.1f, 0.2f);
        var composer = new CameraComposer();

        composer.Apply(camera, 0.016f, new List<CameraInfluence>());

        Assert.Equal(10, camera.Rect.X);
        Assert.Equal(20, camera.Rect.Y);
        Assert.Equal(0.1f, camera.SubpixelOffset.X, 4);
        Assert.Equal(0.2f, camera.SubpixelOffset.Y, 4);
    }

    [Fact]
    public void Apply_SelectsHighestPriorityCenter_AndLatestOnTie()
    {
        var camera = new Camera(100, 100);
        var composer = new CameraComposer();
        var influences = new List<CameraInfluence>
        {
            CameraInfluence.SetCenter(new Vector2(50f, 50f), 1),
            CameraInfluence.SetCenter(new Vector2(80f, 80f), 1),
            CameraInfluence.SetCenter(new Vector2(120f, 130f), 2)
        };

        composer.Apply(camera, 0.016f, influences);

        Assert.Equal(70, camera.Rect.X);
        Assert.Equal(80, camera.Rect.Y);
    }

    [Fact]
    public void Apply_SumsOffsetAndShake_AndUsesFloorWithSubpixel()
    {
        var camera = new Camera(101, 99);
        var composer = new CameraComposer();
        var influences = new List<CameraInfluence>
        {
            CameraInfluence.SetCenter(new Vector2(20f, 40f), 0),
            CameraInfluence.AddOffset(new Vector2(3.75f, -2.25f), 0),
            CameraInfluence.Shake(new Vector2(0.5f, 1.5f), 0)
        };

        composer.Apply(camera, 0.016f, influences);

        Assert.Equal(-27, camera.Rect.X);
        Assert.Equal(-11, camera.Rect.Y);
        Assert.Equal(0.75f, camera.SubpixelOffset.X, 4);
        Assert.Equal(0.75f, camera.SubpixelOffset.Y, 4);
    }

    [Fact]
    public void Apply_UsesHighestPriorityClampBounds()
    {
        var camera = new Camera(50, 50);
        var composer = new CameraComposer();
        var influences = new List<CameraInfluence>
        {
            CameraInfluence.SetCenter(new Vector2(500f, 500f), 1),
            CameraInfluence.ClampBounds(new Rectangle(0, 0, 1000, 1000), 1),
            CameraInfluence.ClampBounds(new Rectangle(0, 0, 100, 100), 2)
        };

        composer.Apply(camera, 0.016f, influences);

        Assert.Equal(50, camera.Rect.X);
        Assert.Equal(50, camera.Rect.Y);
    }

    [Fact]
    public void Apply_WhenBoundsSmallerThanViewport_PinsToMin()
    {
        var camera = new Camera(200, 150);
        var composer = new CameraComposer();
        var influences = new List<CameraInfluence>
        {
            CameraInfluence.SetCenter(new Vector2(999f, 999f), 0),
            CameraInfluence.ClampBounds(new Rectangle(10, 20, 50, 40), 5)
        };

        composer.Apply(camera, 0.016f, influences);

        Assert.Equal(10, camera.Rect.X);
        Assert.Equal(20, camera.Rect.Y);
    }
}
