using IsometricMagic.Engine.Graphics.Effects;

namespace IsometricMagic.Engine.Graphics.OpenGL
{
    public interface IGlPostProcessEffect : IPostProcessEffect
    {
        void Apply(GlRenderContext context, GlRenderTarget input, GlRenderTarget output);
    }
}
