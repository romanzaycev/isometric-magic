using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using IsometricMagic.Engine;
using Newtonsoft.Json;
using SDL2;
using TiledCS;

namespace IsometricMagic.Game.Scenes
{
    public class IsoTest : Scene
    {
        private readonly Dictionary<string, Texture> _tileTextures = new();
        private bool _isDrag;
        private int _startMouseX;
        private int _startMouseY;

        public IsoTest() : base("iso_test", true)
        {
        }

        protected override IEnumerator InitializeAsync()
        {
            var jsonMapData = File.ReadAllText("./resources/data/maps/map0.json");
            yield return true;
            
            var map = JsonConvert.DeserializeObject<TiledMap>(jsonMapData);
            yield return true;

            var tileset = new TiledTileset("./resources/data/sets/grass.tsx");
            yield return true;
            
            if (map != null)
            {
                var mainLayer = map.Layers.First(l => l.name == "main");

                var mapWidth = mainLayer.width;
                var mapHeight = mainLayer.height;

                var tileWidth = map.TileWidth;
                var tileHeight = map.TileHeight;

                var canvasWidth = tileWidth * mapWidth;
                var canvasHeight = tileHeight * mapHeight;
                var isoC = canvasWidth / 2;
                
                for (var y = 0; y < mapHeight; y++)
                {
                    var Yo = (tileHeight / 2) * y;
                    var Xc = isoC - (tileWidth / 2 * y);
                    
                    for (var x = 0; x < mapWidth; x++)
                    {
                        var tileId = mainLayer.data[x * y];

                        if (tileId > 0)
                        {
                            var tile = tileset.Tiles[tileId];
                            Texture tex;

                            if (!_tileTextures.ContainsKey(tile.image.source))
                            {
                                tex = new Texture(tile.image.width, tile.image.height);
                                tex.LoadImage(new AssetItem($"./resources/data/textures/{tile.image.source}"));

                                _tileTextures.Add(tile.image.source, tex);
                            }
                            else
                            {
                                tex = _tileTextures[tile.image.source];
                            }

                            var Xo = Xc + (x * (tileWidth / 2));
                            Yo += tileHeight / 2;
                            
                            var sprite = new Sprite()
                            {
                                Width = tile.image.width,
                                Height = tile.image.height,
                                Position = new Vector2(
                                    Xo - tileWidth / 2,
                                    Yo - tileHeight / 2
                                ),
                                Texture = tex
                            };
                            
                            MainLayer.Add(sprite);
                        }

                        if (y % 3 == 0)
                        {
                            yield return true;
                        }
                    }
                }
            }
        }

        public override void Update()
        {
            // Mouse camera controller copy-paste
            
            if (Input.IsMousePressed(SDL.SDL_BUTTON_LEFT) && !_isDrag)
            {
                _startMouseX = Input.MouseX;
                _startMouseY = Input.MouseY;
                _isDrag = true;
            }

            if (Input.IsMouseReleased(SDL.SDL_BUTTON_LEFT) && _isDrag)
            {
                _startMouseX = 0;
                _startMouseY = 0;
                _isDrag = false;
            }

            if (_isDrag)
            {
                Camera.Rect.X -= _startMouseX - Input.MouseX;
                Camera.Rect.Y -= _startMouseY - Input.MouseY;
            }
            
            _startMouseX = Input.MouseX;
            _startMouseY = Input.MouseY;
        }
    }
}