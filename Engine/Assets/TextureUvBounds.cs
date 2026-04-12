namespace IonMotion.Engine.Assets
{
    public readonly struct TextureUvBounds
    {
        public float MinX { get; }
        public float MinY { get; }
        public float MaxX { get; }
        public float MaxY { get; }

        public TextureUvBounds(float minX, float minY, float maxX, float maxY)
        {
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
        }
    }
}
