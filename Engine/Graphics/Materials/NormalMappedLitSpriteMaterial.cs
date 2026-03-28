using System.Numerics;
using IsometricMagic.Engine.Graphics.OpenGL;
using Silk.NET.OpenGL;

namespace IsometricMagic.Engine.Graphics.Materials
{
    public sealed class NormalMappedLitSpriteMaterial : IGlMaterial
    {
        private GlShaderProgram? _shader;
        public bool Enabled { get; set; } = true;

        public Vector3 AmbientColor = new(0.2f, 0.2f, 0.2f);

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

            var light = GetPrimaryLight(context);
            _shader.SetVector2("u_lightPos", light.Position.X, light.Position.Y);
            _shader.SetVector3("u_lightColor", light.Color.X, light.Color.Y, light.Color.Z);
            _shader.SetFloat("u_lightIntensity", light.Intensity);
            _shader.SetVector3("u_ambient", AmbientColor.X, AmbientColor.Y, AmbientColor.Z);
        }

        public void Unbind(GlRenderContext context)
        {
            context.Gl.ActiveTexture(TextureUnit.Texture1);
            context.Gl.BindTexture(TextureTarget.Texture2D, 0);
            context.Gl.ActiveTexture(TextureUnit.Texture0);
            context.Gl.BindTexture(TextureTarget.Texture2D, 0);
        }

        private static Graphics.Lighting.Light2D GetPrimaryLight(GlRenderContext context)
        {
            if (context.Scene.Lighting.Lights.Count > 0)
            {
                return context.Scene.Lighting.Lights[0];
            }

            return new Graphics.Lighting.Light2D(new Vector2(context.ViewportWidth * 0.5f, context.ViewportHeight * 0.4f));
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
uniform vec2 u_lightPos;
uniform vec3 u_lightColor;
uniform float u_lightIntensity;
uniform vec3 u_ambient;

void main()
{
    vec4 baseColor = texture(u_albedo, v_uv);
    vec3 normalSample = texture(u_normalMap, v_uv).xyz * 2.0 - 1.0;
    vec3 normal = normalize(normalSample);

    vec3 lightDir = normalize(vec3(u_lightPos - v_world, 0.1));
    float diff = max(dot(normal, lightDir), 0.0);
    vec3 lighting = u_ambient + (u_lightColor * diff * u_lightIntensity);
    vec3 color = baseColor.rgb * lighting;

    FragColor = vec4(color, baseColor.a);
}
";
    }
}
