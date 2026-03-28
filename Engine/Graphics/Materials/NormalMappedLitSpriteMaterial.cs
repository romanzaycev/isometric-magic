using System.Numerics;
using IsometricMagic.Engine.Graphics.Lighting;
using IsometricMagic.Engine.Graphics.OpenGL;
using Silk.NET.OpenGL;

namespace IsometricMagic.Engine.Graphics.Materials
{
    public sealed class NormalMappedLitSpriteMaterial : IGlMaterial
    {
        private GlShaderProgram? _shader;
        public bool Enabled { get; set; } = true;

        public Vector3 AmbientColor = new(0.2f, 0.2f, 0.2f);

        private const int MAX_LIGHTS = 8;

        public void Bind(GlRenderContext context, Sprite sprite, GlNativeTexture albedo, GlNativeTexture? normalMap)
        {
            if (_shader == null)
            {
                _shader = new GlShaderProgram(context.Gl, VertexSource, FragmentSource);
            }

            _shader.Use();

            context.Gl.ActiveTexture(TextureUnit.Texture0);
            context.Gl.BindTexture(TextureTarget.Texture2D, albedo.TextureId);
            _shader.SetInt("u_albedo", 0);

            context.Gl.ActiveTexture(TextureUnit.Texture1);
            var normalTextureId = normalMap?.TextureId ?? 0u;
            context.Gl.BindTexture(TextureTarget.Texture2D, normalTextureId);
            _shader.SetInt("u_normalMap", 1);

            var lights = context.Scene.Lighting.Lights;
            var lightCount = lights.Count;

            _shader.SetInt("u_lightCount", lightCount);

            for (var i = 0; i < MAX_LIGHTS; i++)
            {
                if (i < lightCount)
                {
                    var light = lights[i];
                    _shader.SetVector2($"u_lights[{i}].pos", light.Position.X, light.Position.Y);
                    _shader.SetVector3($"u_lights[{i}].color", light.Color.X, light.Color.Y, light.Color.Z);
                    _shader.SetFloat($"u_lights[{i}].intensity", light.Intensity);
                }
                else
                {
                    _shader.SetVector2($"u_lights[{i}].pos", 0f, 0f);
                    _shader.SetVector3($"u_lights[{i}].color", 0f, 0f, 0f);
                    _shader.SetFloat($"u_lights[{i}].intensity", 0f);
                }
            }

            _shader.SetVector3("u_ambient", AmbientColor.X, AmbientColor.Y, AmbientColor.Z);
        }

        public void Unbind(GlRenderContext context)
        {
            context.Gl.ActiveTexture(TextureUnit.Texture1);
            context.Gl.BindTexture(TextureTarget.Texture2D, 0);
            context.Gl.ActiveTexture(TextureUnit.Texture0);
            context.Gl.BindTexture(TextureTarget.Texture2D, 0);
        }

        private const string VertexSource = @"#version 330 core
layout(location = 0) in vec2 a_pos;
layout(location = 1) in vec2 a_uv;
layout(location = 2) in vec2 a_world;

out vec2 v_uv;
out vec2 v_world;

void main()
{
    v_uv = a_uv;
    v_world = a_world;
    gl_Position = vec4(a_pos.xy, 0.0, 1.0);
}
";

        private const string FragmentSource = @"#version 330 core
in vec2 v_uv;
in vec2 v_world;
out vec4 FragColor;

uniform sampler2D u_albedo;
uniform sampler2D u_normalMap;
uniform int u_lightCount;

struct Light {
    vec2 pos;
    vec3 color;
    float intensity;
};

uniform Light u_lights[8];
uniform vec3 u_ambient;

void main()
{
    vec4 baseColor = texture(u_albedo, v_uv);
    vec3 normalSample = texture(u_normalMap, v_uv).xyz * 2.0 - 1.0;
    vec3 normal = normalize(normalSample);

    vec3 totalLighting = u_ambient;

    for (int i = 0; i < 8; i++) {
        if (i >= u_lightCount) break;
        Light light = u_lights[i];
        vec3 lightDir = normalize(vec3(light.pos - v_world, 0.1));
        float diff = max(dot(normal, lightDir), 0.0);
        totalLighting += light.color * diff * light.intensity;
    }

    vec3 color = baseColor.rgb * totalLighting;
    FragColor = vec4(color, baseColor.a);
}
";
    }
}
