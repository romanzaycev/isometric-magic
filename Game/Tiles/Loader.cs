using Newtonsoft.Json;
using IsometricMagic.Engine.Assets;

namespace IsometricMagic.Game.Tiles
{
    public static class Loader
    {
        public static TileSet Load(string name)
        {
            var jsonMapData = ResourceFileSystem.ReadAllText($"resources/data/sets/{name}.json");
            
            return JsonConvert.DeserializeObject<TileSet>(jsonMapData)!;
        }
    }
}
