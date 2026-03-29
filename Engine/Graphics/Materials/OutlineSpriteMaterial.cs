using System;
using IsometricMagic.Engine.Graphics.OpenGL;
using Silk.NET.OpenGL;

namespace IsometricMagic.Engine.Graphics.Materials
{
    public sealed class OutlineSpriteMaterial : IGlMaterial
    {
        private GlShaderProgram? _shader;
        public bool Enabled { get; set; } = true;

        private const int MaxSteps = 12;

        public void Bind(GlRenderContext context, Sprite sprite, GlNativeTexture albedo, GlNativeTexture? normalMap)
        {
            if (_shader == null)
            {
                _shader = new GlShaderProgram(context.Gl, VertexSource, FragmentSource);
            }

            _shader.Use();
            context.Gl.ActiveTexture(TextureUnit.Texture0);
            context.Gl.BindTexture(TextureTarget.Texture2D, albedo.TextureId);
            _shader.SetInt("u_texture", 0);

            var outline = sprite.Outline;
            var clampedThickness = MathF.Max(0f, MathF.Min(outline.ThicknessTexels, MaxSteps));
            var texelX = albedo.Width > 0 ? 1f / albedo.Width : 0f;
            var texelY = albedo.Height > 0 ? 1f / albedo.Height : 0f;

            _shader.SetVector4("u_outlineColor", outline.Color.X, outline.Color.Y, outline.Color.Z, outline.Color.W);
            _shader.SetFloat("u_thickness", clampedThickness);
            _shader.SetVector2("u_texelSize", texelX, texelY);
        }

        public void Unbind(GlRenderContext context)
        {
            context.Gl.BindTexture(TextureTarget.Texture2D, 0);
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
uniform vec4 u_outlineColor;
uniform vec2 u_texelSize;
uniform float u_thickness;

const int MAX_STEPS = 12;

float SampleAlpha(vec2 uv)
{
    vec2 inside = step(vec2(0.0), uv) * step(uv, vec2(1.0));
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
    FragColor = vec4(u_outlineColor.rgb, u_outlineColor.a * edge);
}
";
    }
}
