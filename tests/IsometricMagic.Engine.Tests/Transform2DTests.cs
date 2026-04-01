using System.Numerics;
using IsometricMagic.Engine;

namespace IsometricMagic.Engine.Tests;

public sealed class Transform2DTests
{
    [Fact]
    public void CanvasPosition_WithoutParent_EqualsLocalPosition()
    {
        var entity = new Entity("e");
        entity.Transform.LocalPosition = new Vector2(3f, -4f);

        Assert.Equal(new Vector2(3f, -4f), entity.Transform.CanvasPosition);
    }

    [Fact]
    public void CanvasPosition_WithParentNoRotation_AddsParentTranslation()
    {
        var parent = new Entity("parent");
        parent.Transform.LocalPosition = new Vector2(10f, 20f);

        var child = new Entity("child");
        child.Transform.LocalPosition = new Vector2(2f, -3f);
        child.SetParent(parent, canvasPositionStays: false);

        Assert.Equal(new Vector2(12f, 17f), child.Transform.CanvasPosition);
    }

    [Fact]
    public void CanvasPosition_WithParentRotation_RotatesThenTranslates()
    {
        var parent = new Entity("parent");
        parent.Transform.LocalPosition = new Vector2(5f, 5f);
        parent.Transform.LocalRotation = 0.25d;

        var child = new Entity("child");
        child.Transform.LocalPosition = new Vector2(1f, 0f);
        child.SetParent(parent, canvasPositionStays: false);

        var canvas = child.Transform.CanvasPosition;
        Assert.InRange(canvas.X, 4.999f, 5.001f);
        Assert.InRange(canvas.Y, 5.999f, 6.001f);
    }

    [Fact]
    public void CanvasRotation_WithParent_AddsLocalAndParent()
    {
        var parent = new Entity("parent");
        parent.Transform.LocalRotation = 0.2d;

        var child = new Entity("child");
        child.Transform.LocalRotation = 0.35d;
        child.SetParent(parent, canvasPositionStays: false);

        Assert.Equal(0.55d, child.Transform.CanvasRotation, 10);
    }

    [Fact]
    public void SetParent_WithCanvasPositionStays_PreservesCanvasTransform()
    {
        var child = new Entity("child");
        child.Transform.LocalPosition = new Vector2(12f, 8f);
        child.Transform.LocalRotation = 0.3d;

        var parent = new Entity("parent");
        parent.Transform.LocalPosition = new Vector2(10f, 5f);
        parent.Transform.LocalRotation = 0.25d;

        var canvasPosBefore = child.Transform.CanvasPosition;
        var canvasRotBefore = child.Transform.CanvasRotation;

        child.SetParent(parent, canvasPositionStays: true);

        AssertVector2Near(canvasPosBefore, child.Transform.CanvasPosition);
        Assert.Equal(canvasRotBefore, child.Transform.CanvasRotation, 9);
    }

    [Fact]
    public void SetParent_WithCanvasPositionStaysFalse_PreservesLocalTransform()
    {
        var child = new Entity("child");
        child.Transform.LocalPosition = new Vector2(2f, 3f);
        child.Transform.LocalRotation = 0.4d;

        var parent = new Entity("parent");
        parent.Transform.LocalPosition = new Vector2(10f, 11f);
        parent.Transform.LocalRotation = 0.1d;

        child.SetParent(parent, canvasPositionStays: false);

        Assert.Equal(new Vector2(2f, 3f), child.Transform.LocalPosition);
        Assert.Equal(0.4d, child.Transform.LocalRotation, 10);
    }

    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(0.25, 0.25)]
    [InlineData(1.0, 1.0)]
    [InlineData(1.2, 0.2)]
    [InlineData(1.6, -0.4)]
    [InlineData(-0.25, -0.25)]
    [InlineData(-1.2, -0.2)]
    [InlineData(-1.6, 0.4)]
    public void NormalizeNor_ContractVectors(double input, double expected)
    {
        var actual = MathHelper.NormalizeNor(input);

        Assert.Equal(expected, actual, 10);
    }

    private static void AssertVector2Near(Vector2 expected, Vector2 actual, float eps = 0.001f)
    {
        Assert.InRange(actual.X, expected.X - eps, expected.X + eps);
        Assert.InRange(actual.Y, expected.Y - eps, expected.Y + eps);
    }
}
