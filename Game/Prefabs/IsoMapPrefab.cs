using System.Collections;
using IsometricMagic.Game.Components.Tilemap;
using IsometricMagic.Game.Maps;
using IsometricMagic.Game.Model;
using IsometricMagic.Game.Tiles;
using MapLoader = IsometricMagic.Game.Maps.Loader;
using TileSetLoader = IsometricMagic.Game.Tiles.Loader;

namespace IsometricMagic.Game.Prefabs
{
    public readonly record struct IsoMapPrefabSpec(
        string MapName,
        string EntityName = "Map"
    );

    public readonly record struct IsoMapInstance(
        Entity Entity,
        Map Map,
        TileSet TileSet,
        IsoWorldPositionConverter Converter,
        IsoTilemapRendererComponent TilemapRenderer,
        int WorldLayerBase,
        int LayerStride
    );

    public sealed class IsoMapPrefab
    {
        private readonly IsoMapPrefabSpec _spec;

        public IsoMapPrefab(IsoMapPrefabSpec spec)
        {
            _spec = spec;
        }

        public IEnumerator InstantiateAsync(Scene scene, Action<IsoMapInstance> onInstantiated, Entity? parent = null)
        {
            var map = MapLoader.Load(_spec.MapName);
            yield return true;

            var tileSet = TileSetLoader.Load(map.TileSet);
            yield return true;

            var converter = new IsoWorldPositionConverter(
                map.TileWidth,
                map.TileHeight,
                map.Width,
                map.Height
            );

            var mapEntity = scene.CreateEntity(_spec.EntityName, parent);
            var tilemapRenderer = mapEntity.AddComponent<IsoTilemapRendererComponent>();
            tilemapRenderer.Load(map, tileSet, converter, scene.MainLayer);
            tilemapRenderer.BuildAll();

            var instance = new IsoMapInstance(
                mapEntity,
                map,
                tileSet,
                converter,
                tilemapRenderer,
                tilemapRenderer.WorldLayerBase,
                tilemapRenderer.LayerStride
            );

            onInstantiated(instance);
        }
    }
}
