using System.Numerics;

namespace IsometricMagic.Game.Model
{
    public class IsoWorldPositionConverter
    {
        private readonly int _tileWidth;
        private readonly int _tileHeight;
        private readonly int _mapWidth;
        private readonly int _mapHeight;
        private readonly int _originY;
        public int WorldWidth { get; }
        public int WorldHeight { get; }

        public IsoWorldPositionConverter(int tileWidth, int tileHeight, int mapWidth, int mapHeight)
        {
            _tileWidth = tileWidth;
            _tileHeight = tileHeight;
            _mapWidth = mapWidth;
            _mapHeight = mapHeight;
            _originY = mapHeight * (tileHeight / 2);
            WorldWidth = mapWidth * tileWidth / 2;
            WorldHeight = mapHeight * tileWidth / 2;
        }

        public Vector2 GetCanvasPosition(WorldObject worldObject)
        {
            return GetCanvasPosition(worldObject.WorldPosX, worldObject.WorldPosY);
        }
        
        public Vector2 GetCanvasPosition(Vector2 pos)
        {
            return GetCanvasPosition((int)pos.X, (int)pos.Y);
        }

        public Vector2 GetCanvasPosition(int x, int y)
        {
            return new Vector2(
                x + y,
                _originY + ((y / 2) - (x / 2))
            );
        }
    }
}