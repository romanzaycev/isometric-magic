using System;
using System.Collections.Generic;
using Silk.NET.OpenGL;

namespace IsometricMagic.Engine.Graphics.OpenGL
{
    public sealed class GlShaderProgram
    {
        private readonly GL _gl;
        private readonly Dictionary<string, int> _uniformLocations = new();
        public uint ProgramId { get; }

        public GlShaderProgram(GL gl, string vertexSource, string fragmentSource)
        {
            _gl = gl;
            var vertexShader = CompileShader(ShaderType.VertexShader, vertexSource);
            var fragmentShader = CompileShader(ShaderType.FragmentShader, fragmentSource);

            ProgramId = _gl.CreateProgram();
            _gl.AttachShader(ProgramId, vertexShader);
            _gl.AttachShader(ProgramId, fragmentShader);
            _gl.LinkProgram(ProgramId);

            _gl.GetProgram(ProgramId, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                var info = _gl.GetProgramInfoLog(ProgramId);
                throw new InvalidOperationException($"GL program link error: {info}");
            }

            _gl.DetachShader(ProgramId, vertexShader);
            _gl.DetachShader(ProgramId, fragmentShader);
            _gl.DeleteShader(vertexShader);
            _gl.DeleteShader(fragmentShader);
        }

        public void Use()
        {
            _gl.UseProgram(ProgramId);
        }

        public void SetInt(string name, int value)
        {
            _gl.Uniform1(GetLocation(name), value);
        }

        public void SetFloat(string name, float value)
        {
            _gl.Uniform1(GetLocation(name), value);
        }

        public void SetVector2(string name, float x, float y)
        {
            _gl.Uniform2(GetLocation(name), x, y);
        }

        public void SetVector3(string name, float x, float y, float z)
        {
            _gl.Uniform3(GetLocation(name), x, y, z);
        }

        public void SetVector4(string name, float x, float y, float z, float w)
        {
            _gl.Uniform4(GetLocation(name), x, y, z, w);
        }

        private uint CompileShader(ShaderType type, string source)
        {
            var shader = _gl.CreateShader(type);
            _gl.ShaderSource(shader, source);
            _gl.CompileShader(shader);

            _gl.GetShader(shader, ShaderParameterName.CompileStatus, out var status);
            if (status == 0)
            {
                var info = _gl.GetShaderInfoLog(shader);
                throw new InvalidOperationException($"GL shader compile error ({type}): {info}");
            }

            return shader;
        }

        private int GetLocation(string name)
        {
            if (_uniformLocations.TryGetValue(name, out var location))
            {
                return location;
            }

            location = _gl.GetUniformLocation(ProgramId, name);
            _uniformLocations[name] = location;
            return location;
        }
    }
}
