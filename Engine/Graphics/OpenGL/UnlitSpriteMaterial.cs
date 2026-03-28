using Silk.NET.OpenGL;

namespace IsometricMagic.Engine.Graphics.OpenGL
{
    public sealed class UnlitSpriteMaterial : IGlMaterial
    {
        private readonly GlShaderProgram _shader;
        public bool Enabled { get; set; } = true;

        public UnlitSpriteMaterial(GL gl)
        {
            _shader = new GlShaderProgram(gl, VertexSource, FragmentSource);
        }

        public void Bind(GlRenderContext context, Sprite sprite, GlNativeTexture albedo, GlNativeTexture? normalMap)
        {
            _shader.Use();
            context.Gl.ActiveTexture(TextureUnit.Texture0);
            context.Gl.BindTexture(TextureTarget.Texture2D, albedo.TextureId);
            _shader.SetInt("u_texture", 0);
        }

        public void Unbind(GlRenderContext context)
        {
            context.Gl.BindTexture(TextureTarget.Texture2D, 0);
        }

        private const string VertexSource = @"#version 330 core
layout(location = 0) in vec2 a_pos;
layout(location = 1) in vec2 a_uv;
layout(location = 2) in vec2 a_world;

out vec2 v_uv;

void main()
{
    v_uv = a_uv;
    gl_Position = vec4(a_pos.xy, 0.0, 1.0);
}
";

        private const string FragmentSource = @"#version 330 core
in vec2 v_uv;
out vec4 FragColor;

uniform sampler2D u_texture;

void main()
{
    FragColor = texture(u_texture, v_uv);
}
";
    }
}
