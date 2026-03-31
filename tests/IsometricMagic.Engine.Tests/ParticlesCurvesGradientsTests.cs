using System.Numerics;
using IsometricMagic.Engine.Particles;

namespace IsometricMagic.Engine.Tests;

public sealed class ParticlesCurvesGradientsTests
{
    [Fact]
    public void FloatCurve_SetKeys_WithNull_ResetsToDefaultConstantOne()
    {
        var curve = new FloatCurve(3f);

        curve.SetKeys(null!);

        Assert.Equal(1f, curve.Evaluate(0f));
        Assert.Equal(1f, curve.Evaluate(0.5f));
        Assert.Equal(1f, curve.Evaluate(1f));
    }

    [Fact]
    public void FloatCurve_SetKeys_ClampsSortsAndInterpolates()
    {
        var curve = new FloatCurve();
        curve.Resolution = 3;

        curve.SetKeys(
            new FloatCurve.Key(1.5f, 20f),
            new FloatCurve.Key(-0.2f, 0f)
        );

        Assert.Equal(0f, curve.Evaluate(0f));
        Assert.Equal(10f, curve.Evaluate(0.5f));
        Assert.Equal(20f, curve.Evaluate(1f));
    }

    [Fact]
    public void FloatCurve_SetKeys_WithSingleKey_ExtendsToOne()
    {
        var curve = new FloatCurve();

        curve.SetKeys(new FloatCurve.Key(0.25f, 7f));

        Assert.Equal(7f, curve.Evaluate(0f));
        Assert.Equal(7f, curve.Evaluate(1f));
    }

    [Fact]
    public void FloatCurve_Resolution_IsClampedToAtLeastTwo()
    {
        var curve = new FloatCurve();

        curve.Resolution = 1;

        Assert.Equal(2, curve.Resolution);
    }

    [Fact]
    public void FloatCurve_Evaluate_ClampsInputRange()
    {
        var curve = new FloatCurve();
        curve.Resolution = 3;
        curve.SetKeys(new FloatCurve.Key(0f, 2f), new FloatCurve.Key(1f, 6f));

        Assert.Equal(2f, curve.Evaluate(-100f));
        Assert.Equal(6f, curve.Evaluate(100f));
    }

    [Fact]
    public void ColorGradient_SetKeys_WithNull_ResetsToWhite()
    {
        var gradient = new ColorGradient();
        var red = new Vector4(1f, 0f, 0f, 1f);
        gradient.SetKeys(new ColorGradient.Key(0f, red), new ColorGradient.Key(1f, red));

        gradient.SetKeys(null!);

        AssertVector4(gradient.Evaluate(0f), new Vector4(1f, 1f, 1f, 1f));
        AssertVector4(gradient.Evaluate(1f), new Vector4(1f, 1f, 1f, 1f));
    }

    [Fact]
    public void ColorGradient_SetKeys_ClampsSortsAndInterpolates()
    {
        var gradient = new ColorGradient
        {
            Resolution = 3
        };

        gradient.SetKeys(
            new ColorGradient.Key(1.5f, new Vector4(0f, 1f, 0f, 1f)),
            new ColorGradient.Key(-0.1f, new Vector4(1f, 0f, 0f, 1f))
        );

        AssertVector4(gradient.Evaluate(0f), new Vector4(1f, 0f, 0f, 1f));
        AssertVector4(gradient.Evaluate(0.5f), new Vector4(0.5f, 0.5f, 0f, 1f));
        AssertVector4(gradient.Evaluate(1f), new Vector4(0f, 1f, 0f, 1f));
    }

    [Fact]
    public void ColorGradient_SetKeys_WithSingleKey_ExtendsToOne()
    {
        var gradient = new ColorGradient();
        var color = new Vector4(0.2f, 0.3f, 0.4f, 0.5f);

        gradient.SetKeys(new ColorGradient.Key(0.3f, color));

        AssertVector4(gradient.Evaluate(0f), color);
        AssertVector4(gradient.Evaluate(1f), color);
    }

    [Fact]
    public void ColorGradient_Resolution_IsClampedToAtLeastTwo()
    {
        var gradient = new ColorGradient();

        gradient.Resolution = 1;

        Assert.Equal(2, gradient.Resolution);
    }

    [Fact]
    public void ColorGradient_Evaluate_ClampsInputRange()
    {
        var gradient = new ColorGradient
        {
            Resolution = 3
        };
        var a = new Vector4(0f, 0f, 0f, 1f);
        var b = new Vector4(1f, 1f, 1f, 1f);
        gradient.SetKeys(new ColorGradient.Key(0f, a), new ColorGradient.Key(1f, b));

        AssertVector4(gradient.Evaluate(-10f), a);
        AssertVector4(gradient.Evaluate(10f), b);
    }

    [Fact]
    public void ColorGradient_Evaluate_IsDeterministic_WhenInputsUnchanged()
    {
        var gradient = ColorGradient.Solid(new Vector4(0.3f, 0.6f, 0.9f, 1f));

        var first = gradient.Evaluate(0.4f);
        var second = gradient.Evaluate(0.4f);

        AssertVector4(first, second);
    }

    private static void AssertVector4(Vector4 actual, Vector4 expected, float eps = 0.0001f)
    {
        Assert.InRange(actual.X, expected.X - eps, expected.X + eps);
        Assert.InRange(actual.Y, expected.Y - eps, expected.Y + eps);
        Assert.InRange(actual.Z, expected.Z - eps, expected.Z + eps);
        Assert.InRange(actual.W, expected.W - eps, expected.W + eps);
    }
}
