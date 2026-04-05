using IsometricMagic.Engine.Graphics.Materials;

namespace IsometricMagic.Engine.Graphics.OpenGL
{
    public interface IGlMaterial : IMaterial
    {
        void Bind(
            GlRenderContext context,
            Sprite sprite,
            GlNativeTexture albedo,
            GlNativeTexture? normalMap,
            GlNativeTexture? emissionMap
        );
        void Unbind(GlRenderContext context);
    }
}
