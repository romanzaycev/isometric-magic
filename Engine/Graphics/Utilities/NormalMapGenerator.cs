using System;
using System.Runtime.InteropServices;
using static SDL2.SDL;
using static SDL2.SDL_image;

namespace IsometricMagic.Engine.Graphics.Utilities
{
    public static class NormalMapGenerator
    {
        public static byte[] GenerateFromImage(string imagePath, out int width, out int height, float strength = 1f)
        {
            var surface = IMG_Load(imagePath);
            if (surface == IntPtr.Zero)
            {
                throw new InvalidOperationException($"IMG_Load error: {IMG_GetError()}");
            }

            var targetFormat = BitConverter.IsLittleEndian ? SDL_PIXELFORMAT_ABGR8888 : SDL_PIXELFORMAT_RGBA8888;
            var converted = SDL_ConvertSurfaceFormat(surface, targetFormat, 0);
            SDL_FreeSurface(surface);

            if (converted == IntPtr.Zero)
            {
                throw new InvalidOperationException($"SDL_ConvertSurfaceFormat error: {SDL_GetError()}");
            }

            var surfaceInfo = Marshal.PtrToStructure<SDL_Surface>(converted);
            width = surfaceInfo.w;
            height = surfaceInfo.h;
            var pitch = surfaceInfo.pitch;

            var pixelCount = width * height;
            var luminance = new float[pixelCount];

            SDL_LockSurface(converted);
            unsafe
            {
                var data = (byte*) surfaceInfo.pixels;
                for (var y = 0; y < height; y++)
                {
                    var row = data + (y * pitch);
                    for (var x = 0; x < width; x++)
                    {
                        var idx = x * 4;
                        var r = row[idx + 0];
                        var g = row[idx + 1];
                        var b = row[idx + 2];
                        luminance[y * width + x] = (0.2126f * r + 0.7152f * g + 0.0722f * b) / 255f;
                    }
                }
            }
            SDL_UnlockSurface(converted);
            SDL_FreeSurface(converted);

            var output = new byte[pixelCount * 4];
            for (var y = 0; y < height; y++)
            {
                var y0 = Math.Max(y - 1, 0);
                var y1 = Math.Min(y + 1, height - 1);
                for (var x = 0; x < width; x++)
                {
                    var x0 = Math.Max(x - 1, 0);
                    var x1 = Math.Min(x + 1, width - 1);

                    var tl = luminance[y0 * width + x0];
                    var t = luminance[y0 * width + x];
                    var tr = luminance[y0 * width + x1];
                    var l = luminance[y * width + x0];
                    var r = luminance[y * width + x1];
                    var bl = luminance[y1 * width + x0];
                    var b = luminance[y1 * width + x];
                    var br = luminance[y1 * width + x1];

                    var dx = (tr + 2f * r + br) - (tl + 2f * l + bl);
                    var dy = (bl + 2f * b + br) - (tl + 2f * t + tr);

                    var nx = -dx * strength;
                    var ny = -dy * strength;
                    var nz = 1f;
                    var len = (float) Math.Sqrt(nx * nx + ny * ny + nz * nz);
                    nx /= len;
                    ny /= len;
                    nz /= len;

                    var outIndex = (y * width + x) * 4;
                    output[outIndex + 0] = (byte) Math.Clamp((nx * 0.5f + 0.5f) * 255f, 0f, 255f);
                    output[outIndex + 1] = (byte) Math.Clamp((ny * 0.5f + 0.5f) * 255f, 0f, 255f);
                    output[outIndex + 2] = (byte) Math.Clamp((nz * 0.5f + 0.5f) * 255f, 0f, 255f);
                    output[outIndex + 3] = 255;
                }
            }

            return output;
        }
    }
}
