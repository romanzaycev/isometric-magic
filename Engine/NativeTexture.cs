using IsometricMagic.Engine.Graphics;

namespace IsometricMagic.Engine
{
    public abstract class NativeTexture
    {
        public abstract GraphicsBackend Backend { get; }
    }

    public sealed class GlNativeTexture : NativeTexture
    {
        public override GraphicsBackend Backend => GraphicsBackend.OpenGL;
        public uint TextureId { get; }
        public uint FramebufferId { get; }
        public bool IsRenderTarget { get; }
        public int Width { get; }
        public int Height { get; }

        public GlNativeTexture(uint textureId, uint framebufferId, bool isRenderTarget, int width, int height)
        {
            TextureId = textureId;
            FramebufferId = framebufferId;
            IsRenderTarget = isRenderTarget;
            Width = width;
            Height = height;
        }
    }
}
