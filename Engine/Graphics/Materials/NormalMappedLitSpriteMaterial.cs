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

            var tint = sprite.Color;
            _shader.SetVector4("u_tint", tint.X, tint.Y, tint.Z, tint.W);

            context.Gl.ActiveTexture(TextureUnit.Texture1);
            var normalTextureId = normalMap?.TextureId ?? 0u;
            context.Gl.BindTexture(TextureTarget.Texture2D, normalTextureId);
            _shader.SetInt("u_normalMap", 1);

            var lighting = context.Scene.Lighting;
            var lights = lighting.Lights;
            var lightCount = lights.Count;

            _shader.SetInt("u_lightCount", lightCount);
            _shader.SetVector3("u_ambientColor", lighting.AmbientColor.X, lighting.AmbientColor.Y, lighting.AmbientColor.Z);
            _shader.SetFloat("u_ambientIntensity", lighting.AmbientIntensity);

            for (var i = 0; i < MAX_LIGHTS; i++)
            {
                if (i < lightCount)
                {
                    var light = lights[i];
                    _shader.SetVector2($"u_lights[{i}].pos", light.Position.X, light.Position.Y);
                    _shader.SetVector3($"u_lights[{i}].color", light.Color.X, light.Color.Y, light.Color.Z);
                    _shader.SetFloat($"u_lights[{i}].intensity", light.Intensity);
                    _shader.SetFloat($"u_lights[{i}].radius", light.Radius);
                    _shader.SetFloat($"u_lights[{i}].height", light.Height);
                    _shader.SetFloat($"u_lights[{i}].falloff", light.Falloff);
                    _shader.SetFloat($"u_lights[{i}].innerRadius", light.InnerRadius);
                    _shader.SetFloat($"u_lights[{i}].centerAttenuation", light.CenterAttenuation);
                }
                else
                {
                    _shader.SetVector2($"u_lights[{i}].pos", 0f, 0f);
                    _shader.SetVector3($"u_lights[{i}].color", 0f, 0f, 0f);
                    _shader.SetFloat($"u_lights[{i}].intensity", 0f);
                    _shader.SetFloat($"u_lights[{i}].radius", 0f);
                    _shader.SetFloat($"u_lights[{i}].height", 0f);
                    _shader.SetFloat($"u_lights[{i}].falloff", 0f);
                    _shader.SetFloat($"u_lights[{i}].innerRadius", 0f);
                    _shader.SetFloat($"u_lights[{i}].centerAttenuation", 1f);
                }
            }
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
uniform vec4 u_tint;

uniform vec3 u_ambientColor;
uniform float u_ambientIntensity;

struct Light {
    vec2 pos;
    vec3 color;
    float intensity;
    float radius;
    float height;
    float falloff;
    float innerRadius;
    float centerAttenuation;
};

uniform Light u_lights[8];

void main()
{
    vec4 baseColor = texture(u_albedo, v_uv) * u_tint;
    vec3 normalSample = texture(u_normalMap, v_uv).xyz * 2.0 - 1.0;
    vec3 normal = normalize(normalSample);

    vec3 totalLighting = u_ambientColor * u_ambientIntensity;

    for (int i = 0; i < 8; i++) {
        if (i >= u_lightCount) break;
        Light light = u_lights[i];

        vec2 toLight = light.pos - v_world;
        float dist = length(toLight);
        float safeRadius = max(light.radius, 0.0001);
        float atten = clamp(1.0 - dist / safeRadius, 0.0, 1.0);
        atten = pow(atten, light.falloff);

        if (light.innerRadius > 0.0001) {
            float safeInner = max(light.innerRadius, 0.0001);
            float t = smoothstep(0.0, safeInner, dist);
            float center = mix(light.centerAttenuation, 1.0, t);
            atten *= center;
        }

        vec3 lightDir = normalize(vec3(toLight, light.height));
        float diff = max(dot(normal, lightDir), 0.0);

        totalLighting += light.color * diff * light.intensity * atten;
    }

    totalLighting = min(totalLighting, vec3(1.5));

    vec3 color = baseColor.rgb * totalLighting;
    FragColor = vec4(color, baseColor.a);
}
";
    }
}
