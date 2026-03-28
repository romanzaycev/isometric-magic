using Silk.NET.OpenGL;

namespace IsometricMagic.Engine.Graphics.OpenGL
{
    public sealed class GlRenderContext
    {
        public GL Gl { get; }
        public int ViewportWidth { get; set; }
        public int ViewportHeight { get; set; }
        public float Time { get; set; }
        public Scene Scene { get; set; }
        public Camera Camera { get; set; }
        public GlFullscreenQuad FullscreenQuad { get; }

        public GlRenderContext(GL gl, GlFullscreenQuad fullscreenQuad, Scene scene, Camera camera)
        {
            Gl = gl;
            FullscreenQuad = fullscreenQuad;
            Scene = scene;
            Camera = camera;
        }
    }
}
