using System.Numerics;
using IsometricMagic.Engine;

namespace IsometricMagic.Engine.Tests;

public sealed class Transform2DTests
{
    [Fact]
    public void WorldPosition_WithoutParent_EqualsLocalPosition()
    {
        var entity = new Entity("e");
        entity.Transform.LocalPosition = new Vector2(3f, -4f);

        Assert.Equal(new Vector2(3f, -4f), entity.Transform.WorldPosition);
    }

    [Fact]
    public void WorldPosition_WithParentNoRotation_AddsParentTranslation()
    {
        var parent = new Entity("parent");
        parent.Transform.LocalPosition = new Vector2(10f, 20f);

        var child = new Entity("child");
        child.Transform.LocalPosition = new Vector2(2f, -3f);
        child.SetParent(parent, worldPositionStays: false);

        Assert.Equal(new Vector2(12f, 17f), child.Transform.WorldPosition);
    }

    [Fact]
    public void WorldPosition_WithParentRotation_RotatesThenTranslates()
    {
        var parent = new Entity("parent");
        parent.Transform.LocalPosition = new Vector2(5f, 5f);
        parent.Transform.LocalRotation = 0.25d;

        var child = new Entity("child");
        child.Transform.LocalPosition = new Vector2(1f, 0f);
        child.SetParent(parent, worldPositionStays: false);

        var world = child.Transform.WorldPosition;
        Assert.InRange(world.X, 4.999f, 5.001f);
        Assert.InRange(world.Y, 5.999f, 6.001f);
    }

    [Fact]
    public void WorldRotation_WithParent_AddsLocalAndParent()
    {
        var parent = new Entity("parent");
        parent.Transform.LocalRotation = 0.2d;

        var child = new Entity("child");
        child.Transform.LocalRotation = 0.35d;
        child.SetParent(parent, worldPositionStays: false);

        Assert.Equal(0.55d, child.Transform.WorldRotation, 10);
    }

    [Fact]
    public void SetParent_WithWorldPositionStays_PreservesWorldTransform()
    {
        var child = new Entity("child");
        child.Transform.LocalPosition = new Vector2(12f, 8f);
        child.Transform.LocalRotation = 0.3d;

        var parent = new Entity("parent");
        parent.Transform.LocalPosition = new Vector2(10f, 5f);
        parent.Transform.LocalRotation = 0.25d;

        var worldPosBefore = child.Transform.WorldPosition;
        var worldRotBefore = child.Transform.WorldRotation;

        child.SetParent(parent, worldPositionStays: true);

        AssertVector2Near(worldPosBefore, child.Transform.WorldPosition);
        Assert.Equal(worldRotBefore, child.Transform.WorldRotation, 9);
    }

    [Fact]
    public void SetParent_WithWorldPositionStaysFalse_PreservesLocalTransform()
    {
        var child = new Entity("child");
        child.Transform.LocalPosition = new Vector2(2f, 3f);
        child.Transform.LocalRotation = 0.4d;

        var parent = new Entity("parent");
        parent.Transform.LocalPosition = new Vector2(10f, 11f);
        parent.Transform.LocalRotation = 0.1d;

        child.SetParent(parent, worldPositionStays: false);

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
