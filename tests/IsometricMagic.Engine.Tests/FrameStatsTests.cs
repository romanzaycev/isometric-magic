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
        Assert.Equal(0, stats.SpritesVisited);
        Assert.Equal(0, stats.SpritesSkipped);
        Assert.Equal(0, stats.TextureBinds);
        Assert.Equal(0, stats.TextureLoads);
        Assert.Equal(0, stats.ActiveEntities);
        Assert.Equal(0, stats.ComponentsUpdated);
        Assert.Equal(0, stats.ComponentsLateUpdated);
        Assert.Equal(0f, stats.EventLoopMs);
        Assert.Equal(0f, stats.UpdateCpuMs);
        Assert.Equal(0f, stats.RenderCpuMs);
        Assert.Equal(0f, stats.SleepMs);
        Assert.Equal(0, stats.GcAllocBytes);

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
    public void EndFrame_CapturesLastCompletedFrameCounters()
    {
        var stats = FrameStats.GetInstance();
        ResetStats(stats);

        stats.BeginFrame();
        stats.AddDrawCall();
        stats.AddSpriteDrawn();
        stats.AddSpriteCulled();
        stats.AddSpriteVisited();
        stats.AddSpriteSkipped();
        stats.AddTextureBind(42);
        stats.AddTextureBind(0);
        stats.AddTextureLoad();
        stats.SetActiveEntities(7);
        stats.AddComponentUpdated();
        stats.AddComponentLateUpdated();
        stats.SetEventLoopMs(1.5f);
        stats.SetUpdateCpuMs(2.5f);
        stats.SetRenderCpuMs(3.5f);
        stats.SetSleepMs(4.5f);
        stats.EndFrame(0.016f);

        Assert.Equal(1, stats.LastDrawCalls);
        Assert.Equal(1, stats.LastSpritesDrawn);
        Assert.Equal(1, stats.LastSpritesCulled);
        Assert.Equal(1, stats.LastSpritesVisited);
        Assert.Equal(1, stats.LastSpritesSkipped);
        Assert.Equal(1, stats.LastTextureBinds);
        Assert.Equal(1, stats.LastTextureLoads);
        Assert.Equal(7, stats.LastActiveEntities);
        Assert.Equal(1, stats.LastComponentsUpdated);
        Assert.Equal(1, stats.LastComponentsLateUpdated);
        Assert.Equal(1.5f, stats.LastEventLoopMs, 3);
        Assert.Equal(2.5f, stats.LastUpdateCpuMs, 3);
        Assert.Equal(3.5f, stats.LastRenderCpuMs, 3);
        Assert.Equal(4.5f, stats.LastSleepMs, 3);
        Assert.Equal(stats.GcAllocBytes, stats.LastGcAllocBytes);

        stats.BeginFrame();
        Assert.Equal(0, stats.DrawCalls);
        Assert.Equal(1, stats.LastDrawCalls);
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
        SetProperty(stats, nameof(FrameStats.SpritesVisited), 0);
        SetProperty(stats, nameof(FrameStats.SpritesSkipped), 0);
        SetProperty(stats, nameof(FrameStats.TextureBinds), 0);
        SetProperty(stats, nameof(FrameStats.TextureLoads), 0);
        SetProperty(stats, nameof(FrameStats.ActiveEntities), 0);
        SetProperty(stats, nameof(FrameStats.ComponentsUpdated), 0);
        SetProperty(stats, nameof(FrameStats.ComponentsLateUpdated), 0);
        SetProperty(stats, nameof(FrameStats.EventLoopMs), 0f);
        SetProperty(stats, nameof(FrameStats.UpdateCpuMs), 0f);
        SetProperty(stats, nameof(FrameStats.RenderCpuMs), 0f);
        SetProperty(stats, nameof(FrameStats.SleepMs), 0f);
        SetProperty(stats, nameof(FrameStats.GcAllocBytes), 0L);
        SetProperty(stats, nameof(FrameStats.LastDrawCalls), 0);
        SetProperty(stats, nameof(FrameStats.LastSpritesDrawn), 0);
        SetProperty(stats, nameof(FrameStats.LastSpritesCulled), 0);
        SetProperty(stats, nameof(FrameStats.LastSpritesVisited), 0);
        SetProperty(stats, nameof(FrameStats.LastSpritesSkipped), 0);
        SetProperty(stats, nameof(FrameStats.LastTextureBinds), 0);
        SetProperty(stats, nameof(FrameStats.LastTextureLoads), 0);
        SetProperty(stats, nameof(FrameStats.LastActiveEntities), 0);
        SetProperty(stats, nameof(FrameStats.LastComponentsUpdated), 0);
        SetProperty(stats, nameof(FrameStats.LastComponentsLateUpdated), 0);
        SetProperty(stats, nameof(FrameStats.LastEventLoopMs), 0f);
        SetProperty(stats, nameof(FrameStats.LastUpdateCpuMs), 0f);
        SetProperty(stats, nameof(FrameStats.LastRenderCpuMs), 0f);
        SetProperty(stats, nameof(FrameStats.LastSleepMs), 0f);
        SetProperty(stats, nameof(FrameStats.LastGcAllocBytes), 0L);
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
