using System.Reflection;
using IsometricMagic.Engine.Diagnostics;
using IsometricMagic.Engine.Graphics;

namespace IsometricMagic.Engine.Tests;

public sealed class FrameStatsTests
{
    [Fact]
    public void BeginFrame_ResetsCounters_AndAddMethodsIncrement()
    {
        var stats = FrameStats.GetInstance();
        ResetStats(stats);

        stats.AddDrawCall();
        stats.AddSpriteDrawn();
        stats.AddSpriteCulled();
        stats.BeginFrame();

        Assert.Equal(0, stats.DrawCalls);
        Assert.Equal(0, stats.SpritesDrawn);
        Assert.Equal(0, stats.SpritesCulled);

        stats.AddDrawCall();
        stats.AddSpriteDrawn();
        stats.AddSpriteCulled();

        Assert.Equal(1, stats.DrawCalls);
        Assert.Equal(1, stats.SpritesDrawn);
        Assert.Equal(1, stats.SpritesCulled);
    }

    [Fact]
    public void EndFrame_WithNonPositiveDelta_DoesNotUpdateTiming()
    {
        var stats = FrameStats.GetInstance();
        ResetStats(stats);

        stats.EndFrame(0f);

        Assert.Equal(0f, stats.FrameMs);
        Assert.Equal(0f, stats.FrameMsAvg);
        Assert.Equal(0f, stats.FpsAvg);
    }

    [Fact]
    public void EndFrame_UpdatesFrameMs_AndSamplingWindow()
    {
        var stats = FrameStats.GetInstance();
        ResetStats(stats);

        stats.EndFrame(0.1f);
        Assert.Equal(100f, stats.FrameMs, 4);
        Assert.Equal(0f, stats.FrameMsAvg);
        Assert.Equal(0f, stats.FpsAvg);

        stats.EndFrame(0.1f);
        stats.EndFrame(0.1f);
        stats.EndFrame(0.1f);
        stats.EndFrame(0.1f);

        Assert.Equal(100f, stats.FrameMsAvg, 3);
        Assert.Equal(10f, stats.FpsAvg, 3);
    }

    [Fact]
    public void MetadataSetters_UpdateProperties()
    {
        var stats = FrameStats.GetInstance();

        stats.SetViewport(1920, 1080);
        stats.SetBackend(GraphicsBackend.OpenGL);
        stats.SetVSync(true);
        stats.SetSceneName("Gameplay");

        Assert.Equal(1920, stats.ViewportWidth);
        Assert.Equal(1080, stats.ViewportHeight);
        Assert.Equal(GraphicsBackend.OpenGL, stats.Backend);
        Assert.True(stats.VSync);
        Assert.Equal("Gameplay", stats.SceneName);
    }

    private static void ResetStats(FrameStats stats)
    {
        SetField(stats, "_sampleTime", 0d);
        SetField(stats, "_sampleFrames", 0);
        SetProperty(stats, nameof(FrameStats.FrameMs), 0f);
        SetProperty(stats, nameof(FrameStats.FrameMsAvg), 0f);
        SetProperty(stats, nameof(FrameStats.FpsAvg), 0f);
        stats.BeginFrame();
    }

    private static void SetField<T>(FrameStats stats, string name, T value)
    {
        var field = typeof(FrameStats).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field.SetValue(stats, value);
    }

    private static void SetProperty<T>(FrameStats stats, string name, T value)
    {
        var property = typeof(FrameStats).GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(property);
        property.SetValue(stats, value);
    }
}
