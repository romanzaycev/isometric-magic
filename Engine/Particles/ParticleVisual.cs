using System;
using IsometricMagic.Engine;
using IsometricMagic.Engine.Graphics.Materials;

namespace IsometricMagic.Engine.Particles
{
    public sealed class ParticleVisual
    {
        public Texture Texture { get; }
        public IMaterial? Material { get; set; }
        public int Width { get; }
        public int Height { get; }
        public OriginPoint OriginPoint { get; set; } = OriginPoint.Centered;
        public int SortingOffset { get; set; }

        public ParticleVisual(Texture texture, int width, int height)
        {
            Texture = texture ?? throw new ArgumentNullException(nameof(texture));
            Width = width;
            Height = height;
        }

        public static ParticleVisual FromSprite(Sprite sprite)
        {
            if (sprite.Texture == null)
            {
                throw new ArgumentException("Sprite texture is required.", nameof(sprite));
            }

            return new ParticleVisual(sprite.Texture, sprite.Width, sprite.Height)
            {
                Material = sprite.Material,
                OriginPoint = sprite.OriginPoint,
                SortingOffset = sprite.Sorting
            };
        }
    }
}
