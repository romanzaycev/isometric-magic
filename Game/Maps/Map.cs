using System.Collections.Generic;

namespace IsometricMagic.Game.Maps
{
    [Newtonsoft.Json.JsonObject]
    public class Map
    {
        [Newtonsoft.Json.JsonProperty("name")]
        public string Name { get; private set; }

        [Newtonsoft.Json.JsonProperty("width")]
        public int Width { get; private set; }

        [Newtonsoft.Json.JsonProperty("height")]
        public int Height { get; private set; }

        [Newtonsoft.Json.JsonProperty("tileWidth")]
        public int TileWidth { get; private set; }

        [Newtonsoft.Json.JsonProperty("tileHeight")]
        public int TileHeight { get; private set; }

        [Newtonsoft.Json.JsonProperty("tileSet")]
        public string TileSet { get; private set; }

        [Newtonsoft.Json.JsonProperty("layers")]
        public MapLayer[] Layers { get; private set; }
    }
}