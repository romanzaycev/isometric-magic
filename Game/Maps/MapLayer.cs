namespace IsometricMagic.Game.Maps
{
    [Newtonsoft.Json.JsonObject]
    public class MapLayer
    {
        [Newtonsoft.Json.JsonProperty("name")]
        public required string Name { get; init; }

        [Newtonsoft.Json.JsonProperty("data")]
        public required int[] Data { get; init; }
    }
}