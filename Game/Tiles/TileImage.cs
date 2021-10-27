namespace IsometricMagic.Game.Tiles
{
    [Newtonsoft.Json.JsonObject]
    public class TileImage
    {
        [Newtonsoft.Json.JsonProperty("width")]
        public int Width { get; private set; }

        [Newtonsoft.Json.JsonProperty("height")]
        public int Height { get; private set; }
        
        [Newtonsoft.Json.JsonProperty("source")]
        public string Source { get; private set; }
    }
}