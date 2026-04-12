using Newtonsoft.Json;
using IonMotion.Engine.Assets;

namespace IonMotion.Game.Tiles
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
