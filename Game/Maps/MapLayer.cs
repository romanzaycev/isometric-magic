namespace IsometricMagic.Game.Maps
{
    [Newtonsoft.Json.JsonObject]
    public class MapLayer
    {
        [Newtonsoft.Json.JsonProperty("name")]
        public string Name { get; private set; }

        [Newtonsoft.Json.JsonProperty("data")]
        public int[] Data { get; private set; }
    }
}