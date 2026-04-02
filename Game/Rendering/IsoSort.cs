using IsometricMagic.Game.Model;

namespace IsometricMagic.Game.Rendering
{
    public static class IsoSort
    {
        public const int DepthScale = 10000;

        public const int BiasFloor = 0;
        public const int BiasObject = 1000;
        public const int BiasActor = 5000;
        public const int BiasVfx = 10000;

        public static int FromCanvas(CanvasPosition canvasPos, int layerBase, int bias)
        {
            var x = (int) MathF.Round(canvasPos.X);
            var y = (int) MathF.Round(canvasPos.Y);
            return layerBase + y * DepthScale + x + bias;
        }

        public static int CalculateLayerStride(int mapWidth, int mapHeight, int tileWidth, int tileHeight)
        {
            var scaleFactor = tileWidth / tileHeight;
            if (scaleFactor <= 0)
            {
                scaleFactor = 1;
            }

            var worldWidth = mapWidth * tileWidth / scaleFactor;
            var worldHeight = mapHeight * tileWidth / scaleFactor;
            var originY = mapHeight * tileHeight / 2;

            var maxCanvasY = originY + worldHeight / scaleFactor;
            var minCanvasY = originY - worldWidth / scaleFactor;
            var maxCanvasX = worldWidth + worldHeight;

            var maxSorting = maxCanvasY * DepthScale + maxCanvasX;
            var minSorting = minCanvasY * DepthScale;
            var range = Math.Abs(maxSorting - minSorting);

            return range + DepthScale * 4;
        }
    }
}
