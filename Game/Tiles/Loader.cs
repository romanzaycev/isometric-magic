using Newtonsoft.Json;
using IsometricMagic.Engine.Assets;

namespace IsometricMagic.Game.Tiles
{
    public static class Loader
    {
        public static TileSet Load(string name)
        {
            var jsonMapData = ResourceFileSystem.ReadAllText(ResourceFileSystem.Data($"sets/{name}.json"));
            
            return JsonConvert.DeserializeObject<TileSet>(jsonMapData)!;
        }
    }
}
