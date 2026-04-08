namespace IsometricMagic.Engine.Assets
{
    public readonly struct TextureRegion
    {
        public int X { get; }
        public int Y { get; }
        public int Width { get; }
        public int Height { get; }

        public TextureRegion(int x, int y, int width, int height)
        {
            if (x < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(x), "Texture region x must be non-negative.");
            }

            if (y < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(y), "Texture region y must be non-negative.");
            }

            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "Texture region width must be greater than zero.");
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height), "Texture region height must be greater than zero.");
            }

            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }
}
