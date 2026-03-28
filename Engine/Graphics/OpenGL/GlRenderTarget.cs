namespace IsometricMagic.Engine.Graphics.OpenGL
{
    public readonly struct GlRenderTarget
    {
        public uint FramebufferId { get; }
        public uint TextureId { get; }
        public int Width { get; }
        public int Height { get; }

        public GlRenderTarget(uint framebufferId, uint textureId, int width, int height)
        {
            FramebufferId = framebufferId;
            TextureId = textureId;
            Width = width;
            Height = height;
        }
    }
}
