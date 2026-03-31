using Silk.NET.OpenGL;
using IsometricMagic.Engine.Diagnostics;

namespace IsometricMagic.Engine.Graphics.OpenGL
{
    public sealed class GlFullscreenQuad
    {
        private static readonly FrameStats FrameStats = FrameStats.GetInstance();
        private readonly GL _gl;
        public uint Vao { get; }
        public uint Vbo { get; }

        public GlFullscreenQuad(GL gl)
        {
            _gl = gl;

            var vertices = new float[]
            {
                -1f, -1f, 0f, 0f,
                1f, -1f, 1f, 0f,
                1f, 1f, 1f, 1f,
                -1f, -1f, 0f, 0f,
                1f, 1f, 1f, 1f,
                -1f, 1f, 0f, 1f
            };

            Vao = _gl.GenVertexArray();
            Vbo = _gl.GenBuffer();

            _gl.BindVertexArray(Vao);
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, Vbo);
            unsafe
            {
                fixed (float* data = vertices)
                {
                    _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertices.Length * sizeof(float)), data,
                        BufferUsageARB.StaticDraw);
                }
            }

            _gl.EnableVertexAttribArray(0);
            _gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);

            _gl.EnableVertexAttribArray(1);
            _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float),
                2 * sizeof(float));

            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
            _gl.BindVertexArray(0);
        }

        public void Draw()
        {
            _gl.BindVertexArray(Vao);
            FrameStats.AddDrawCall();
            _gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
            _gl.BindVertexArray(0);
        }
    }
}
