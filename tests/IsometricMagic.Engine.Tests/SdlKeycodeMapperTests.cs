using System.Reflection;
using IsometricMagic.Engine;
using SDL2;

namespace IsometricMagic.Engine.Tests;

public sealed class SdlKeycodeMapperTests
{
    [Fact]
    public void TryMap_KnownKeycodes_ReturnsExpectedKey()
    {
        Assert.True(SdlKeycodeMapper.TryMap(SDL.SDL_Keycode.SDLK_a, out var a));
        Assert.Equal(Key.A, a);

        Assert.True(SdlKeycodeMapper.TryMap(SDL.SDL_Keycode.SDLK_F3, out var f3));
        Assert.Equal(Key.F3, f3);

        Assert.True(SdlKeycodeMapper.TryMap(SDL.SDL_Keycode.SDLK_LEFT, out var left));
        Assert.Equal(Key.Left, left);
    }

    [Fact]
    public void TryMap_UnknownKeycode_ReturnsFalse_AndDoesNotThrow()
    {
        var unknown = (SDL.SDL_Keycode) int.MaxValue;

        var mapped = SdlKeycodeMapper.TryMap(unknown, out _);

        Assert.False(mapped);
    }

    [Fact]
    public void TryMap_UnknownKeycode_IsLoggedOncePerKeycode_ByInternalTracking()
    {
        var set = GetUnknownLoggedSet();
        set.Clear();
        var unknown = (SDL.SDL_Keycode) 1234567;

        Assert.False(SdlKeycodeMapper.TryMap(unknown, out _));
        Assert.False(SdlKeycodeMapper.TryMap(unknown, out _));

        Assert.Single(set);
    }

    private static HashSet<SDL.SDL_Keycode> GetUnknownLoggedSet()
    {
        var field = typeof(SdlKeycodeMapper).GetField("UnknownLogged", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(field);
        var value = field.GetValue(null);
        Assert.NotNull(value);
        return (HashSet<SDL.SDL_Keycode>) value;
    }
}
