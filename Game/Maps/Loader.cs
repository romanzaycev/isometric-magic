using Newtonsoft.Json;
using IsometricMagic.Engine.Assets;

namespace IsometricMagic.Game.Maps
{
    public static class Loader
    {
        public static Map Load(string name)
        {
            var jsonMapData = ResourceFileSystem.ReadAllText($"resources/data/maps/{name}.json");
            
            return JsonConvert.DeserializeObject<Map>(jsonMapData)!;
        }
    }
}
