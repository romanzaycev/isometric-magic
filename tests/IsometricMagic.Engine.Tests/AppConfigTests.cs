using IsometricMagic.Engine;
using IsometricMagic.Engine.Graphics;

namespace IsometricMagic.Engine.Tests;

public sealed class AppConfigTests
{
    [Fact]
    public void MissingSectionsAndKeys_UseDefaults_AndDoNotThrow()
    {
        var path = CreateIni(string.Empty);

        try
        {
            var config = new AppConfig(path);

            Assert.Equal(800, config.WindowWidth);
            Assert.Equal(600, config.WindowHeight);
            Assert.Equal(60, config.TargetFps);
            Assert.False(config.VSync);
            Assert.False(config.IsFullscreen);
            Assert.Equal(GraphicsBackend.OpenGL, config.GraphicsBackend);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void InvalidIntegerValues_FallbackToDefaults()
    {
        var path = CreateIni("""
[Window]
Width=abc
Height=

[Engine]
TargetFPS=not-int
""");

        try
        {
            var config = new AppConfig(path);

            Assert.Equal(800, config.WindowWidth);
            Assert.Equal(600, config.WindowHeight);
            Assert.Equal(60, config.TargetFps);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("1", true)]
    [InlineData("on", true)]
    [InlineData("false", false)]
    [InlineData("0", false)]
    [InlineData("off", false)]
    [InlineData("junk", false)]
    public void BoolParsing_UsesExpectedTokens(string token, bool expected)
    {
        var path = CreateIni($"""
[Engine]
VSync={token}

[Window]
Fullscreen={token}
""");

        try
        {
            var config = new AppConfig(path);

            Assert.Equal(expected, config.VSync);
            Assert.Equal(expected, config.IsFullscreen);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void KeyParsing_ValidAndInvalidValues()
    {
        var validPath = CreateIni("""
[DebugOverlay]
ToggleKey=F2
""");
        var invalidPath = CreateIni("""
[DebugOverlay]
ToggleKey=NoSuchKey
""");

        try
        {
            var valid = new AppConfig(validPath);
            var invalid = new AppConfig(invalidPath);

            Assert.Equal(Key.F2, valid.DebugOverlayToggleKey);
            Assert.Equal(Key.F3, invalid.DebugOverlayToggleKey);
        }
        finally
        {
            File.Delete(validPath);
            File.Delete(invalidPath);
        }
    }

    [Fact]
    public void GraphicsBackend_SupportsOpenGlAndGl_AndThrowsForUnsupported()
    {
        var openGlPath = CreateIni("""
[Graphics]
Backend=OpenGL
""");
        var glPath = CreateIni("""
[Graphics]
Backend=gl
""");
        var invalidPath = CreateIni("""
[Graphics]
Backend=vulkan
""");

        try
        {
            var openGl = new AppConfig(openGlPath);
            var gl = new AppConfig(glPath);

            Assert.Equal(GraphicsBackend.OpenGL, openGl.GraphicsBackend);
            Assert.Equal(GraphicsBackend.OpenGL, gl.GraphicsBackend);

            var invalid = new AppConfig(invalidPath);
            var ex = Assert.Throws<InvalidOperationException>(() => _ = invalid.GraphicsBackend);
            Assert.Contains("Unsupported graphics backend", ex.Message);
        }
        finally
        {
            File.Delete(openGlPath);
            File.Delete(glPath);
            File.Delete(invalidPath);
        }
    }

    [Fact]
    public void RuntimeEditor_ConfigParsing_UsesDefaultsAndProvidedValues()
    {
        var defaultsPath = CreateIni(string.Empty);
        var customPath = CreateIni("""
[RuntimeEditor]
Enabled=true
ToggleKey=F6
Port=6123
OpenBrowser=false
BrowserAppMode=true
BrowserExecutable=google-chrome
""");

        try
        {
            var defaults = new AppConfig(defaultsPath);
            var custom = new AppConfig(customPath);

            Assert.False(defaults.RuntimeEditorEnabled);
            Assert.Equal(Key.F4, defaults.RuntimeEditorToggleKey);
            Assert.Equal(5057, defaults.RuntimeEditorPort);
            Assert.True(defaults.RuntimeEditorOpenBrowser);
            Assert.False(defaults.RuntimeEditorBrowserAppMode);
            Assert.Equal("chromium", defaults.RuntimeEditorBrowserExecutable);

            Assert.True(custom.RuntimeEditorEnabled);
            Assert.Equal(Key.F6, custom.RuntimeEditorToggleKey);
            Assert.Equal(6123, custom.RuntimeEditorPort);
            Assert.False(custom.RuntimeEditorOpenBrowser);
            Assert.True(custom.RuntimeEditorBrowserAppMode);
            Assert.Equal("google-chrome", custom.RuntimeEditorBrowserExecutable);
        }
        finally
        {
            File.Delete(defaultsPath);
            File.Delete(customPath);
        }
    }

    [Fact]
    public void Properties_AreStableAcrossRepeatedReads()
    {
        var path = CreateIni("""
[Window]
Width=1024
Height=768
""");

        try
        {
            var config = new AppConfig(path);

            var firstWidth = config.WindowWidth;
            var secondWidth = config.WindowWidth;
            var firstHeight = config.WindowHeight;
            var secondHeight = config.WindowHeight;

            Assert.Equal(firstWidth, secondWidth);
            Assert.Equal(firstHeight, secondHeight);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string CreateIni(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"isometric_magic_tests_{Guid.NewGuid():N}.ini");
        File.WriteAllText(path, content);
        return path;
    }
}
