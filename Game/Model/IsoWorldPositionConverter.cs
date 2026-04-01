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

        public CanvasPosition ToCanvas(IsoWorldPosition worldPos)
        {
            return new CanvasPosition(
                worldPos.Y + worldPos.X,
                _originY + (worldPos.Y / _scaleFactor - worldPos.X / _scaleFactor)
            );
        }

        public CanvasPosition ToIsoTileCanvas(int tileX, int tileY)
        {
            return ToCanvas(new IsoWorldPosition(
                tileX * _tileWidth / (float)_scaleFactor,
                tileY * _tileWidth / (float)_scaleFactor
            ));
        }

        public IsoWorldPosition ToIsoWorld(CanvasPosition canvasPos)
        {
            var cYOffsets = canvasPos.Y - _originY;
            var cXScaled = canvasPos.X / _scaleFactor;

            return new IsoWorldPosition(
                cXScaled - cYOffsets - _tileFactor,
                cXScaled + cYOffsets + _tileFactor
            );
        }
    }
}
