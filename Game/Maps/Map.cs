namespace IsometricMagic.Game.Maps
{
    [Newtonsoft.Json.JsonObject]
    public class Map
    {
        [Newtonsoft.Json.JsonProperty("name")]
        public required string Name { get; init; }

        [Newtonsoft.Json.JsonProperty("width")]
        public int Width { get; private set; }

        [Newtonsoft.Json.JsonProperty("height")]
        public int Height { get; private set; }

        [Newtonsoft.Json.JsonProperty("tileWidth")]
        public int TileWidth { get; private set; }

        [Newtonsoft.Json.JsonProperty("tileHeight")]
        public int TileHeight { get; private set; }

        [Newtonsoft.Json.JsonProperty("tileSet")]
        public required string TileSet { get; init; }

        [Newtonsoft.Json.JsonProperty("layers")]
        public required MapLayer[] Layers { get; init; }
    }
}