using IsometricMagic.Engine.Graphics.OpenGL;
using Silk.NET.OpenGL;

namespace IsometricMagic.Engine.Graphics.Effects
{
    public sealed class VignetteEffect : IGlPostProcessEffect
    {
        private GlShaderProgram? _shader;
        public bool Enabled { get; set; } = true;

        public float Intensity { get; set; } = 0.5f;
        public float Radius { get; set; } = 0.75f;
        public float Softness { get; set; } = 0.25f;

        public void Apply(GlRenderContext context, GlRenderTarget input, GlRenderTarget output)
        {
            if (_shader == null)
            {
                _shader = new GlShaderProgram(context.Gl, VertexSource, FragmentSource);
            }

            var gl = context.Gl;
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, output.FramebufferId);
            gl.Viewport(0, 0, (uint) output.Width, (uint) output.Height);

            _shader.Use();
            _shader.SetInt("u_texture", 0);
            _shader.SetVector2("u_resolution", output.Width, output.Height);
            _shader.SetFloat("u_intensity", Intensity);
            _shader.SetFloat("u_radius", Radius);
            _shader.SetFloat("u_softness", Softness);

            gl.ActiveTexture(TextureUnit.Texture0);
            gl.BindTexture(TextureTarget.Texture2D, input.TextureId);
            context.FullscreenQuad.Draw();
            gl.BindTexture(TextureTarget.Texture2D, 0);
        }

        private const string VertexSource = @"#version 330 core
layout(location = 0) in vec2 a_pos;
layout(location = 1) in vec2 a_uv;

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
uniform vec2 u_resolution;
uniform float u_intensity;
uniform float u_radius;
uniform float u_softness;

void main()
{
    vec4 color = texture(u_texture, v_uv);
    vec2 coord = v_uv * 2.0 - 1.0;
    float dist = length(coord);
    float vignette = smoothstep(u_radius, u_radius + u_softness, dist);
    float strength = mix(1.0, 1.0 - u_intensity, vignette);
    FragColor = vec4(color.rgb * strength, color.a);
}
";
    }
}
