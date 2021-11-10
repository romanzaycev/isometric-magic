using System.IO;
using Newtonsoft.Json;

namespace IsometricMagic.Game.Maps
{
    public static class Loader
    {
        public static Map Load(string name)
        {
            var jsonMapData = File.ReadAllText($"./resources/data/maps/{name}.json");
            
            return JsonConvert.DeserializeObject<Map>(jsonMapData);
        }
    }
}