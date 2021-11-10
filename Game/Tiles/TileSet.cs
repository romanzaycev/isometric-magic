using System.Collections.Generic;

namespace IsometricMagic.Game.Tiles
{
    [Newtonsoft.Json.JsonObject]
    public class TileSet
    {
        [Newtonsoft.Json.JsonProperty("name")]
        public string Name { get; private set; }

        [Newtonsoft.Json.JsonProperty("tiles")]
        private List<Tile> _jsonTiles = new();

        private bool _isTilesInitialized;

        private Dictionary<int, Tile> _tiles = new();

        public IReadOnlyDictionary<int, Tile> Tiles
        {
            get
            {
                if (_isTilesInitialized) return _tiles;
                
                foreach (var tile in _jsonTiles)
                {
                    _tiles.Add(tile.Id, tile);
                }
                    
                _isTilesInitialized = true;

                return _tiles;
            }
        }
    }
}