using System;
using IsometricMagic.Engine.Assets;
using IsometricMagic.Engine.Diagnostics;
using IsometricMagic.Engine.Graphics.OpenGL;
using Silk.NET.OpenGL;

namespace IsometricMagic.Engine.Graphics.Materials
{
    public sealed class OutlineSpriteMaterial : IGlMaterial
    {
        private static readonly FrameStats FrameStats = FrameStats.GetInstance();
        private GlShaderProgram? _shader;
        private bool _samplerInitialized;
        public bool Enabled { get; set; } = true;

        private const int MaxSteps = 12;

        public void Bind(
            GlRenderContext context,
            Sprite sprite,
            GlNativeTexture albedo,
            GlNativeTexture? normalMap,
            GlNativeTexture? emissionMap
        )
        {
            if (_shader == null)
            {
                _shader = new GlShaderProgram(context.Gl, VertexSource, FragmentSource);
                _samplerInitialized = false;
            }

            _shader.Use();
            if (!_samplerInitialized)
            {
                _shader.SetInt("u_texture", 0);
                _samplerInitialized = true;
            }

            context.Gl.ActiveTexture(TextureUnit.Texture0);
            context.Gl.BindTexture(TextureTarget.Texture2D, albedo.TextureId);
            FrameStats.AddTextureBind(albedo.TextureId);

            var outline = sprite.Outline;
            var clampedThickness = MathF.Max(0f, MathF.Min(outline.ThicknessTexels, MaxSteps));
            var texelX = albedo.Width > 0 ? 1f / albedo.Width : 0f;
            var texelY = albedo.Height > 0 ? 1f / albedo.Height : 0f;
            var uvBounds = TextureUvMapper.Resolve(sprite.Region, albedo.Width, albedo.Height);

            _shader.SetVector4("u_outlineColor", outline.Color.X, outline.Color.Y, outline.Color.Z, outline.Color.W);
            _shader.SetFloat("u_thickness", clampedThickness);
            _shader.SetVector2("u_texelSize", texelX, texelY);
            _shader.SetVector2("u_uvMin", uvBounds.MinX, uvBounds.MinY);
            _shader.SetVector2("u_uvMax", uvBounds.MaxX, uvBounds.MaxY);
        }

        public void Unbind(GlRenderContext context)
        {
        }

        private const string VertexSource = @"#version 330 core
layout(location = 0) in vec2 a_pos;
layout(location = 1) in vec2 a_uv;
layout(location = 3) in vec4 a_color;

out vec2 v_uv;
out vec4 v_color;

void main()
{
    v_uv = a_uv;
    v_color = a_color;
    gl_Position = vec4(a_pos.xy, 0.0, 1.0);
}
";

        private const string FragmentSource = @"#version 330 core
in vec2 v_uv;
in vec4 v_color;
out vec4 FragColor;

uniform sampler2D u_texture;
uniform vec4 u_outlineColor;
uniform vec2 u_texelSize;
uniform float u_thickness;
uniform vec2 u_uvMin;
uniform vec2 u_uvMax;

const int MAX_STEPS = 12;

float SampleAlpha(vec2 uv)
{
    vec2 inside = step(u_uvMin, uv) * step(uv, u_uvMax);
    float mask = inside.x * inside.y;
    return texture(u_texture, uv).a * mask;
}

void main()
{
    float baseAlpha = SampleAlpha(v_uv);
    float maxAlpha = baseAlpha;
    int steps = int(ceil(u_thickness));

    for (int i = 1; i <= MAX_STEPS; i++)
    {
        if (i > steps) break;
        vec2 o = u_texelSize * float(i);

        maxAlpha = max(maxAlpha, SampleAlpha(v_uv + vec2( o.x, 0.0)));
        maxAlpha = max(maxAlpha, SampleAlpha(v_uv + vec2(-o.x, 0.0)));
        maxAlpha = max(maxAlpha, SampleAlpha(v_uv + vec2(0.0,  o.y)));
        maxAlpha = max(maxAlpha, SampleAlpha(v_uv + vec2(0.0, -o.y)));
        maxAlpha = max(maxAlpha, SampleAlpha(v_uv + vec2( o.x,  o.y)));
        maxAlpha = max(maxAlpha, SampleAlpha(v_uv + vec2(-o.x,  o.y)));
        maxAlpha = max(maxAlpha, SampleAlpha(v_uv + vec2( o.x, -o.y)));
        maxAlpha = max(maxAlpha, SampleAlpha(v_uv + vec2(-o.x, -o.y)));
    }

    float edge = clamp(maxAlpha - baseAlpha, 0.0, 1.0);
    vec4 color = u_outlineColor * v_color;
    FragColor = vec4(color.rgb, color.a * edge);
}
";
    }
}
