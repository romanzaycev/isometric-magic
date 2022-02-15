using System.Numerics;

namespace IsometricMagic.Game.Model
{
    public class IsoWorldPositionConverter
    {
        public const int WORLD_BORDER_THRESHOLD = 30;
        public int WorldWidth { get; }
        public int WorldHeight { get; }
        private readonly int _tileWidth;
        private readonly int _tileHeight;
        private readonly int _mapWidth;
        private readonly int _mapHeight;
        private readonly int _originY;
        private readonly int _scaleFactor;
        private readonly int _tileFactor;

        public IsoWorldPositionConverter(int tileWidth, int tileHeight, int mapWidth, int mapHeight)
        {
            _tileWidth = tileWidth;
            _tileHeight = tileHeight;
            _mapWidth = mapWidth;
            _mapHeight = mapHeight;
            _originY = mapHeight * tileHeight / 2; // Half height size
            _scaleFactor = tileWidth / tileHeight;
            _tileFactor = tileHeight / _scaleFactor;
            WorldWidth = mapWidth * tileWidth / _scaleFactor;
            WorldHeight = mapHeight * tileWidth / _scaleFactor;
        }

        public Vector2 GetCanvasPosition(WorldObject worldObject)
        {
            return GetCanvasPosition(worldObject.WorldPosX, worldObject.WorldPosY);
        }

        public Vector2 GetCanvasPosition(Vector2 worldPos)
        {
            return GetCanvasPosition((int) worldPos.X, (int) worldPos.Y);
        }

        public Vector2 GetCanvasPosition(int worldX, int worldY)
        {
            return new Vector2(
                worldY + worldX,
                _originY + (worldY / _scaleFactor - worldX / _scaleFactor)
            );
        }

        public Vector2 GetTilePosition(int tileX, int tileY)
        {
            return GetCanvasPosition(tileX * _tileWidth / _scaleFactor, tileY * _tileWidth / _scaleFactor);
        }

        public Vector2 GetWorldPosition(int canvasX, int canvasY)
        {
            var cYOffsets = canvasY - _originY;
            var cXScaled = canvasX / _scaleFactor;
            
            return new Vector2(
                cXScaled - cYOffsets - _tileFactor,
                cXScaled + cYOffsets + _tileFactor
            );
        }
    }
}