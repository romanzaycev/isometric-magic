using IonMotion.Engine.Graphics.Effects;

namespace IonMotion.Engine.Graphics.OpenGL
{
    public interface IGlPostProcessEffect : IPostProcessEffect
    {
        void Apply(GlRenderContext context, GlRenderTarget input, GlRenderTarget output);
    }
}
