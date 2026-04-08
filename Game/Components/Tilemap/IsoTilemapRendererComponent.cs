using IsometricMagic.Game.Maps;
using IsometricMagic.Game.Model;
using IsometricMagic.Game.Components.Spatial;
using IsometricMagic.Game.Tiles;
using IsometricMagic.Game.Rendering;

namespace IsometricMagic.Game.Components.Tilemap
{
    public class IsoTilemapRendererComponent : WorldPositionConverterProviderComponent
    {
        private Map _map = null!;
        private TileSet _tileSet = null!;
        private IsoWorldPositionConverter _converter = null!;
        public override IsoWorldPositionConverter? Converter => _converter;
        private SceneLayer _targetLayer = null!;
        private int _layerStride;

        public int LayerStride => _layerStride;
        public int WorldLayerBase => _layerStride * _map.Layers.Length;

        private readonly Dictionary<string, LayerState> _layerStates = new();
        private readonly Dictionary<string, Texture> _textureCache = new();

        public void Load(Map map, TileSet tileSet, IsoWorldPositionConverter converter, SceneLayer targetLayer)
        {
            _map = map;
            _tileSet = tileSet;
            _converter = converter;
            _targetLayer = targetLayer;
            _layerStride = IsoSort.CalculateLayerStride(map.Width, map.Height, map.TileWidth, map.TileHeight);
        }

        public void BuildAll()
        {
            _layerStates.Clear();

            for (var layerIndex = 0; layerIndex < _map.Layers.Length; layerIndex++)
            {
                var layer = _map.Layers[layerIndex];
                BuildLayerInternal(layer, layerIndex);
            }
        }

        private void BuildLayerInternal(MapLayer layer, int layerIndex)
        {
            var state = new LayerState
            {
                Name = layer.Name,
                Width = _map.Width,
                Height = _map.Height,
                TileIds = new int[layer.Data.Length]
            };

            for (var i = 0; i < layer.Data.Length; i++)
            {
                state.TileIds[i] = layer.Data[i];
            }

            state.Sprites = new Sprite?[layer.Data.Length];

            var layerOffset = CalculateLayerBase(layerIndex);
            var mat = SpriteMaterialFactory.LitAutoNormal();

            for (var y = 0; y < _map.Height; y++)
            {
                for (var x = _map.Width - 1; x >= 0; x--)
                {
                    var tileId = state.TileIds[y * _map.Width + (_map.Width - 1 - x)];
                    if (tileId <= 0) continue;

                    var sprite = CreateTileSprite(tileId, x, y, layerOffset, mat);
                    state.Sprites[y * _map.Width + (_map.Width - 1 - x)] = sprite;
                    _targetLayer.Add(sprite);
                }
            }

            _layerStates[layer.Name] = state;
        }

        public void SetTile(string layerName, int x, int y, int tileId)
        {
            if (!_layerStates.TryGetValue(layerName, out var state)) return;
            if (x < 0 || x >= state.Width || y < 0 || y >= state.Height) return;

            var index = y * state.Width + x;
            var currentTileId = state.TileIds[index];

            if (currentTileId == tileId) return;

            state.TileIds[index] = tileId;
            var sprite = state.Sprites[index];

            if (tileId <= 0)
            {
                if (sprite != null)
                {
                    _targetLayer.Remove(sprite);
                    state.Sprites[index] = null;
                }
                return;
            }

            if (sprite == null)
            {
                var layerOffset = CalculateLayerBase(GetLayerIndex(layerName));
                var mat = SpriteMaterialFactory.LitAutoNormal();
                sprite = CreateTileSprite(tileId, x, y, layerOffset, mat);
                state.Sprites[index] = sprite;
                _targetLayer.Add(sprite);
            }
            else
            {
                var layerOffset = CalculateLayerBase(GetLayerIndex(layerName));
                var tile = _tileSet.Tiles[tileId];
                var tex = GetOrLoadTexture(tile);

                sprite.Texture = tex;
                sprite.Width = tile.Image.Width;
                sprite.Height = tile.Image.Height;
                var canvasPos = _converter.ToIsoTileCanvas(x, y);
                sprite.Position = canvasPos.ToVector2();
                sprite.Sorting = CalculateSortIndex(canvasPos, layerOffset);
            }
        }

        public int GetTile(string layerName, int x, int y)
        {
            if (!_layerStates.TryGetValue(layerName, out var state)) return -1;
            if (x < 0 || x >= state.Width || y < 0 || y >= state.Height) return -1;
            return state.TileIds[y * state.Width + x];
        }

        public void SetLayerVisible(string layerName, bool visible)
        {
            if (!_layerStates.TryGetValue(layerName, out var state)) return;
            state.Visible = visible;

            foreach (var sprite in state.Sprites)
            {
                if (sprite != null) sprite.Visible = visible;
            }
        }

        private int GetLayerIndex(string layerName)
        {
            for (var i = 0; i < _map.Layers.Length; i++)
            {
                if (_map.Layers[i].Name == layerName) return i;
            }
            return -1;
        }

        private int CalculateLayerBase(int layerIndex)
        {
            return layerIndex * _layerStride;
        }

        private static int CalculateSortIndex(CanvasPosition canvasPos, int layerOffset)
        {
            return IsoSort.FromCanvas(canvasPos, layerOffset, IsoSort.BiasFloor);
        }

        private Sprite CreateTileSprite(int tileId, int tileX, int tileY, int layerOffset, StandardSpriteMaterial mat)
        {
            var tile = _tileSet.Tiles[tileId];
            var tex = GetOrLoadTexture(tile);

            var position = _converter.ToIsoTileCanvas(tileX, tileY);
            var sprite = new Sprite
            {
                Width = tile.Image.Width,
                Height = tile.Image.Height,
                Position = position.ToVector2(),
                Texture = tex,
                Sorting = CalculateSortIndex(position, layerOffset),
                OriginPoint = OriginPoint.LeftBottom
            };
            sprite.Material = mat;

            return sprite;
        }

        private Texture GetOrLoadTexture(Tile tile)
        {
            if (_textureCache.TryGetValue(tile.Image.Source, out var cached))
            {
                return cached;
            }

            var tex = new Texture(tile.Image.Width, tile.Image.Height);
            tex.LoadImage($"./resources/data/textures/{tile.Image.Source}");
            _textureCache[tile.Image.Source] = tex;
            return tex;
        }

        protected override void OnDestroy()
        {
            
            _layerStates.Clear();
            _textureCache.Clear();
        }

        private class LayerState
        {
            public string Name = string.Empty;
            public int Width;
            public int Height;
            public int[] TileIds = null!;
            public Sprite?[] Sprites = null!;
            public bool Visible = true;
        }
    }
}
