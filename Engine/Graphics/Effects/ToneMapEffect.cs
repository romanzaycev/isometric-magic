using IsometricMagic.Engine.Graphics.OpenGL;
using Silk.NET.OpenGL;

namespace IsometricMagic.Engine.Graphics.Effects
{
    public sealed class ToneMapEffect : IGlPostProcessEffect
    {
        private GlShaderProgram? _shader;
        public bool Enabled { get; set; } = true;

        public float Exposure { get; set; } = 0.6f;
        public float Gamma { get; set; } = 2.2f;
        public float Contrast { get; set; } = 1.15f;

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
            _shader.SetFloat("u_exposure", Exposure);
            _shader.SetFloat("u_gamma", Gamma);
            _shader.SetFloat("u_contrast", Contrast);

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
uniform float u_exposure;
uniform float u_gamma;
uniform float u_contrast;

vec3 TonemapAces(vec3 x)
{
    const float a = 2.51;
    const float b = 0.03;
    const float c = 2.43;
    const float d = 0.59;
    const float e = 0.14;
    return clamp((x * (a * x + b)) / (x * (c * x + d) + e), 0.0, 1.0);
}

void main()
{
    vec4 color = texture(u_texture, v_uv);
    vec3 hdr = color.rgb * u_exposure;
    vec3 mapped = TonemapAces(hdr);
    mapped = pow(mapped, vec3(1.0 / max(u_gamma, 0.0001)));
    mapped = (mapped - vec3(0.5)) * max(u_contrast, 0.0) + vec3(0.5);
    mapped = clamp(mapped, 0.0, 1.0);
    FragColor = vec4(mapped, color.a);
}
";
    }
}
