using System;
using System.Numerics;
using IsometricMagic.Engine;
using IsometricMagic.Engine.Graphics.Lighting;
using IsometricMagic.Engine.Graphics.OpenGL;
using Silk.NET.OpenGL;

namespace IsometricMagic.Engine.Graphics.Materials
{
    public sealed class EmissiveNormalMappedLitSpriteMaterial : IGlMaterial
    {
        private GlShaderProgram? _shader;
        public bool Enabled { get; set; } = true;

        public Vector3 EmissionColor = new(1f, 1f, 1f);
        public float EmissionIntensity = 0f;

        private Texture? _emissionMapTexture;
        private string? _emissionMapPath;
        private Texture? _emissionMapFromPath;
        private string? _emissionMapLoadedPath;

        private const int MAX_LIGHTS = 8;

        public Texture? EmissionMapTexture
        {
            get => _emissionMapTexture;
            set
            {
                if (_emissionMapTexture == value)
                {
                    return;
                }

                _emissionMapTexture = value;
                if (_emissionMapTexture != null)
                {
                    ReleasePathEmissionMap();
                }
            }
        }

        public string? EmissionMapPath
        {
            get => _emissionMapPath;
            set
            {
                if (string.Equals(_emissionMapPath, value, StringComparison.Ordinal))
                {
                    return;
                }

                _emissionMapPath = value;
                ReleasePathEmissionMap();
            }
        }

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

            var emissionMap = ResolveEmissionMapTexture();
            context.Gl.ActiveTexture(TextureUnit.Texture2);
            var emissionTextureId = emissionMap?.TextureId ?? 0u;
            context.Gl.BindTexture(TextureTarget.Texture2D, emissionTextureId);
            _shader.SetInt("u_emissionMap", 2);
            _shader.SetInt("u_hasEmissionMap", emissionMap != null ? 1 : 0);

            _shader.SetVector3("u_emissionColor", EmissionColor.X, EmissionColor.Y, EmissionColor.Z);
            _shader.SetFloat("u_emissionIntensity", EmissionIntensity);

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
            context.Gl.ActiveTexture(TextureUnit.Texture2);
            context.Gl.BindTexture(TextureTarget.Texture2D, 0);
            context.Gl.ActiveTexture(TextureUnit.Texture1);
            context.Gl.BindTexture(TextureTarget.Texture2D, 0);
            context.Gl.ActiveTexture(TextureUnit.Texture0);
            context.Gl.BindTexture(TextureTarget.Texture2D, 0);
        }

        private GlNativeTexture? ResolveEmissionMapTexture()
        {
            if (_emissionMapTexture != null)
            {
                return TextureHolder.GetInstance().GetNativeTexture(_emissionMapTexture) as GlNativeTexture;
            }

            var path = _emissionMapPath;
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            if (_emissionMapFromPath == null ||
                !string.Equals(_emissionMapLoadedPath, path, StringComparison.Ordinal))
            {
                ReleasePathEmissionMap();
                var tex = new Texture(1, 1);
                tex.LoadImage(path);
                _emissionMapFromPath = tex;
                _emissionMapLoadedPath = path;
            }

            return TextureHolder.GetInstance().GetNativeTexture(_emissionMapFromPath) as GlNativeTexture;
        }

        private void ReleasePathEmissionMap()
        {
            if (_emissionMapFromPath == null)
            {
                _emissionMapLoadedPath = null;
                return;
            }

            _emissionMapFromPath.Destroy();
            _emissionMapFromPath = null;
            _emissionMapLoadedPath = null;
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
uniform sampler2D u_emissionMap;
uniform int u_lightCount;
uniform int u_hasEmissionMap;

uniform vec3 u_ambientColor;
uniform float u_ambientIntensity;

uniform vec3 u_emissionColor;
uniform float u_emissionIntensity;

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
    vec4 baseColor = texture(u_albedo, v_uv);
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

    vec3 litColor = baseColor.rgb * totalLighting;
    vec3 emissionMask = vec3(1.0);
    if (u_hasEmissionMap == 1) {
        emissionMask = texture(u_emissionMap, v_uv).rgb;
    }
    vec3 emission = u_emissionColor * u_emissionIntensity * emissionMask * baseColor.a;
    FragColor = vec4(litColor + emission, baseColor.a);
}
";
    }
}
