namespace IsometricMagic.Game.Tiles
{
    [Newtonsoft.Json.JsonObject]
    public class Tile
    {
        [Newtonsoft.Json.JsonProperty("id")]
        public int Id { get; private set; }
        
        [Newtonsoft.Json.JsonProperty("image")]
        public required TileImage Image { get; init; }
    }
}