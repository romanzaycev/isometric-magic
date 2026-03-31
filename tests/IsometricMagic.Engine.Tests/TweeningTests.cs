using System.Numerics;
using IsometricMagic.Engine.Tweening;

namespace IsometricMagic.Engine.Tests;

public sealed class TweeningTests
{
    [Fact]
    public void Delay_WithNonPositiveSeconds_CompletesImmediatelyAndReturnsInvalidHandle()
    {
        var manager = new TweenManager();
        var completed = 0;

        var handle = manager.Delay(0f, () => completed++);

        Assert.False(handle.IsValid);
        Assert.Equal(1, completed);
    }

    [Fact]
    public void Delay_CompletesAfterAccumulatedTime()
    {
        var manager = new TweenManager();
        var completed = 0;

        var handle = manager.Delay(1f, () => completed++);

        Assert.True(handle.IsValid);
        manager.Update(0.4f);
        Assert.Equal(0, completed);
        manager.Update(0.59f);
        Assert.Equal(0, completed);
        manager.Update(0.01f);
        Assert.Equal(1, completed);
        Assert.False(handle.IsValid);
    }

    [Fact]
    public void To_WithNonPositiveDurationAndDelay_SetsTargetImmediatelyAndReturnsInvalid()
    {
        var manager = new TweenManager();
        var value = 2f;
        var completed = 0;

        var handle = manager.To(() => value, v => value = v, 10f, 0f, 0f, null, () => completed++);

        Assert.False(handle.IsValid);
        Assert.Equal(10f, value);
        Assert.Equal(1, completed);
    }

    [Fact]
    public void To_WithDelay_DoesNotCallSetterBeforeDelay()
    {
        var manager = new TweenManager();
        var value = 1f;
        var setCalls = 0;

        manager.To(() => value, v =>
        {
            value = v;
            setCalls++;
        }, 9f, duration: 1f, delay: 0.5f);

        manager.Update(0.49f);

        Assert.Equal(0, setCalls);
        Assert.Equal(1f, value);
    }

    [Fact]
    public void To_UsesEaseAndLerpAndCompletesOnce()
    {
        var manager = new TweenManager();
        var value = 0f;
        var completed = 0;

        manager.To(
            () => value,
            v => value = v,
            to: 10f,
            duration: 2f,
            delay: 0f,
            ease: Easing.InQuad,
            onComplete: () => completed++
        );

        manager.Update(1f);
        Assert.Equal(2.5f, value, 3);
        Assert.Equal(0, completed);

        manager.Update(1f);
        Assert.Equal(10f, value, 3);
        Assert.Equal(1, completed);

        manager.Update(1f);
        Assert.Equal(1, completed);
    }

    [Fact]
    public void Cancel_InvalidatesHandleAndStopsFurtherUpdates()
    {
        var manager = new TweenManager();
        var value = 0f;

        var handle = manager.To(() => value, v => value = v, 10f, 1f);
        Assert.True(handle.IsValid);

        handle.Cancel();

        Assert.False(handle.IsValid);
        manager.Update(1f);
        Assert.Equal(0f, value);
    }

    [Fact]
    public void Interp_Clamp01_ClampsCorrectly()
    {
        Assert.Equal(0f, Interp.Clamp01(-1f));
        Assert.Equal(0.4f, Interp.Clamp01(0.4f));
        Assert.Equal(1f, Interp.Clamp01(2f));
    }

    [Fact]
    public void Easing_Functions_HaveCorrectEndpoints()
    {
        var easings = new EaseFunc[]
        {
            Easing.Linear,
            Easing.InQuad,
            Easing.OutQuad,
            Easing.InOutQuad,
            Easing.InCubic,
            Easing.OutCubic,
            Easing.InOutCubic
        };

        foreach (var easing in easings)
        {
            Assert.Equal(0f, easing(0f), 6);
            Assert.Equal(1f, easing(1f), 6);
        }
    }

    [Fact]
    public void To_Vector2_Interpolates()
    {
        var manager = new TweenManager();
        var value = Vector2.Zero;

        manager.To(() => value, v => value = v, new Vector2(10f, -4f), 1f);
        manager.Update(0.5f);

        Assert.Equal(5f, value.X, 3);
        Assert.Equal(-2f, value.Y, 3);
    }
}
