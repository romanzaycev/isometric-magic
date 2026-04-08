namespace IsometricMagic.Engine.Assets
{
    public static class TextureUvMapper
    {
        public static TextureUvBounds Resolve(TextureRegion? region, int textureWidth, int textureHeight)
        {
            if (textureWidth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(textureWidth), "Texture width must be greater than zero.");
            }

            if (textureHeight <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(textureHeight), "Texture height must be greater than zero.");
            }

            if (!region.HasValue)
            {
                return new TextureUvBounds(0f, 0f, 1f, 1f);
            }

            var value = region.Value;
            var minX = value.X / (float) textureWidth;
            var minY = value.Y / (float) textureHeight;
            var maxX = (value.X + value.Width) / (float) textureWidth;
            var maxY = (value.Y + value.Height) / (float) textureHeight;

            return new TextureUvBounds(minX, minY, maxX, maxY);
        }

        public static TextureUvBounds Expand(TextureUvBounds bounds, float padX, float padY)
        {
            return new TextureUvBounds(
                bounds.MinX - padX,
                bounds.MinY - padY,
                bounds.MaxX + padX,
                bounds.MaxY + padY);
        }
    }
}
