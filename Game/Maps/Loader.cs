using Newtonsoft.Json;
using IonMotion.Engine.Assets;

namespace IonMotion.Game.Maps
{
    public static class Loader
    {
        public static Map Load(string name)
        {
            var jsonMapData = ResourceFileSystem.ReadAllText(ResourceFileSystem.Data($"maps/{name}.json"));
            
            return JsonConvert.DeserializeObject<Map>(jsonMapData)!;
        }
    }
}
