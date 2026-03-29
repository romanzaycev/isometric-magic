using System;
using IsometricMagic.Engine.Graphics.OpenGL;
using Silk.NET.OpenGL;

namespace IsometricMagic.Engine.Graphics.Effects
{
    public sealed class BloomEffect : IGlPostProcessEffect
    {
        private GlShaderProgram? _prefilterShader;
        private GlShaderProgram? _blurShader;
        private GlShaderProgram? _compositeShader;

        private GlRenderTarget _pingTarget;
        private GlRenderTarget _pongTarget;
        private bool _targetsReady;
        private int _targetWidth;
        private int _targetHeight;

        public bool Enabled { get; set; } = true;

        public float Threshold { get; set; } = 1.2f;
        public float Knee { get; set; } = 0.5f;
        public float Intensity { get; set; } = 0.8f;
        public int BlurIterations { get; set; } = 4;

        public void Apply(GlRenderContext context, GlRenderTarget input, GlRenderTarget output)
        {
            if (_prefilterShader == null)
            {
                _prefilterShader = new GlShaderProgram(context.Gl, VertexSource, PrefilterFragmentSource);
                _blurShader = new GlShaderProgram(context.Gl, VertexSource, BlurFragmentSource);
                _compositeShader = new GlShaderProgram(context.Gl, VertexSource, CompositeFragmentSource);
            }

            EnsureTargets(context.Gl, input.Width, input.Height);

            var gl = context.Gl;

            gl.BindFramebuffer(FramebufferTarget.Framebuffer, _pingTarget.FramebufferId);
            gl.Viewport(0, 0, (uint) _pingTarget.Width, (uint) _pingTarget.Height);
            _prefilterShader!.Use();
            _prefilterShader.SetInt("u_texture", 0);
            _prefilterShader.SetFloat("u_threshold", Threshold);
            _prefilterShader.SetFloat("u_knee", Knee);
            gl.ActiveTexture(TextureUnit.Texture0);
            gl.BindTexture(TextureTarget.Texture2D, input.TextureId);
            context.FullscreenQuad.Draw();

            var iterations = BlurIterations < 1 ? 1 : BlurIterations;
            for (var i = 0; i < iterations; i++)
            {
                gl.BindFramebuffer(FramebufferTarget.Framebuffer, _pongTarget.FramebufferId);
                gl.Viewport(0, 0, (uint) _pongTarget.Width, (uint) _pongTarget.Height);
                _blurShader!.Use();
                _blurShader.SetInt("u_texture", 0);
                _blurShader.SetVector2("u_texelSize", 1f / _pingTarget.Width, 1f / _pingTarget.Height);
                _blurShader.SetVector2("u_direction", 1f, 0f);
                gl.ActiveTexture(TextureUnit.Texture0);
                gl.BindTexture(TextureTarget.Texture2D, _pingTarget.TextureId);
                context.FullscreenQuad.Draw();

                gl.BindFramebuffer(FramebufferTarget.Framebuffer, _pingTarget.FramebufferId);
                gl.Viewport(0, 0, (uint) _pingTarget.Width, (uint) _pingTarget.Height);
                _blurShader.SetVector2("u_texelSize", 1f / _pongTarget.Width, 1f / _pongTarget.Height);
                _blurShader.SetVector2("u_direction", 0f, 1f);
                gl.ActiveTexture(TextureUnit.Texture0);
                gl.BindTexture(TextureTarget.Texture2D, _pongTarget.TextureId);
                context.FullscreenQuad.Draw();
            }

            gl.BindFramebuffer(FramebufferTarget.Framebuffer, output.FramebufferId);
            gl.Viewport(0, 0, (uint) output.Width, (uint) output.Height);
            _compositeShader!.Use();
            _compositeShader.SetInt("u_texture", 0);
            _compositeShader.SetInt("u_bloom", 1);
            _compositeShader.SetFloat("u_intensity", Intensity);
            gl.ActiveTexture(TextureUnit.Texture0);
            gl.BindTexture(TextureTarget.Texture2D, input.TextureId);
            gl.ActiveTexture(TextureUnit.Texture1);
            gl.BindTexture(TextureTarget.Texture2D, _pingTarget.TextureId);
            context.FullscreenQuad.Draw();

            gl.ActiveTexture(TextureUnit.Texture1);
            gl.BindTexture(TextureTarget.Texture2D, 0);
            gl.ActiveTexture(TextureUnit.Texture0);
            gl.BindTexture(TextureTarget.Texture2D, 0);
        }

        private void EnsureTargets(GL gl, int width, int height)
        {
            var targetWidth = Math.Max(1, width / 2);
            var targetHeight = Math.Max(1, height / 2);
            if (_targetsReady && targetWidth == _targetWidth && targetHeight == _targetHeight)
            {
                return;
            }

            if (_targetsReady)
            {
                DeleteRenderTarget(gl, _pingTarget);
                DeleteRenderTarget(gl, _pongTarget);
            }

            _pingTarget = CreateRenderTarget(gl, targetWidth, targetHeight);
            _pongTarget = CreateRenderTarget(gl, targetWidth, targetHeight);
            _targetsReady = true;
            _targetWidth = targetWidth;
            _targetHeight = targetHeight;
        }

        private static GlRenderTarget CreateRenderTarget(GL gl, int width, int height)
        {
            var textureId = CreateHdrTexture(gl, width, height);
            var framebufferId = gl.GenFramebuffer();
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, framebufferId);
            gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2D, textureId, 0);

            var status = gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (status != GLEnum.FramebufferComplete)
            {
                throw new InvalidOperationException($"Framebuffer incomplete: {status}");
            }

            gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            return new GlRenderTarget(framebufferId, textureId, width, height);
        }

        private static void DeleteRenderTarget(GL gl, GlRenderTarget target)
        {
            if (target.FramebufferId != 0)
            {
                gl.DeleteFramebuffer(target.FramebufferId);
            }

            if (target.TextureId != 0)
            {
                gl.DeleteTexture(target.TextureId);
            }
        }

        private static uint CreateHdrTexture(GL gl, int width, int height)
        {
            var textureId = gl.GenTexture();
            gl.BindTexture(TextureTarget.Texture2D, textureId);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) GLEnum.Linear);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) GLEnum.Linear);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int) GLEnum.ClampToEdge);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int) GLEnum.ClampToEdge);

            unsafe
            {
                gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba16f, (uint) width, (uint) height, 0,
                    Silk.NET.OpenGL.PixelFormat.Rgba, PixelType.HalfFloat, null);
            }

            gl.BindTexture(TextureTarget.Texture2D, 0);
            return textureId;
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

        private const string PrefilterFragmentSource = @"#version 330 core
in vec2 v_uv;
out vec4 FragColor;

uniform sampler2D u_texture;
uniform float u_threshold;
uniform float u_knee;

void main()
{
    vec3 color = texture(u_texture, v_uv).rgb;
    float brightness = max(max(color.r, color.g), color.b);
    float knee = max(u_knee, 0.0001);
    float soft = clamp((brightness - u_threshold + knee) / (2.0 * knee), 0.0, 1.0);
    float softKnee = soft * soft * (3.0 - 2.0 * soft);
    float contribution = max(brightness - u_threshold, 0.0);
    contribution = max(contribution, softKnee * knee);
    float scale = contribution / max(brightness, 0.0001);
    FragColor = vec4(color * scale, 1.0);
}
";

        private const string BlurFragmentSource = @"#version 330 core
in vec2 v_uv;
out vec4 FragColor;

uniform sampler2D u_texture;
uniform vec2 u_texelSize;
uniform vec2 u_direction;

void main()
{
    vec2 offset = u_direction * u_texelSize;
    vec3 result = texture(u_texture, v_uv).rgb * 0.227027;
    result += texture(u_texture, v_uv + offset).rgb * 0.316216;
    result += texture(u_texture, v_uv - offset).rgb * 0.316216;
    result += texture(u_texture, v_uv + offset * 2.0).rgb * 0.070270;
    result += texture(u_texture, v_uv - offset * 2.0).rgb * 0.070270;
    FragColor = vec4(result, 1.0);
}
";

        private const string CompositeFragmentSource = @"#version 330 core
in vec2 v_uv;
out vec4 FragColor;

uniform sampler2D u_texture;
uniform sampler2D u_bloom;
uniform float u_intensity;

void main()
{
    vec4 baseColor = texture(u_texture, v_uv);
    vec3 bloom = texture(u_bloom, v_uv).rgb * u_intensity;
    FragColor = vec4(baseColor.rgb + bloom, baseColor.a);
}
";
    }
}
