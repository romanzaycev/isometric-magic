using System.IO;
using Newtonsoft.Json;

namespace IsometricMagic.Game.Tiles
{
    public static class Loader
    {
        public static TileSet Load(string name)
        {
            var jsonMapData = File.ReadAllText($"./resources/data/sets/{name}.json");
            
            return JsonConvert.DeserializeObject<TileSet>(jsonMapData);
        }
    }
}