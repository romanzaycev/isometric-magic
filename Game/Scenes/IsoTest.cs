using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using IsometricMagic.Engine;
using IsometricMagic.Game.Controllers.Camera;

namespace IsometricMagic.Game.Scenes
{
    public class IsoTest : Scene
    {
        private readonly Dictionary<string, Texture> _tileTextures = new();

        public IsoTest() : base("iso_test", true)
        {
        }

        protected override IEnumerator InitializeAsync()
        {
            // Camera setup
            Camera.SetController(new MouseController());
            
            // Map setup
            var map = Maps.Loader.Load("map0");
            yield return true;

            var tileSet = Tiles.Loader.Load("grass");
            yield return true;

            if (map != null)
            {
                var mainLayer = map.Layers.First(l => l.Name == "main");

                var mapWidth = map.Width;
                var mapHeight = map.Height;

                var tileWidth = map.TileWidth;
                var tileHeight = map.TileHeight;

                var i = 0;

                for (var y = 0; y < mapHeight; y++)
                {
                    for (var x = mapWidth - 1; x >= 0; x--)
                    {
                        var tileId = mainLayer.Data[i];

                        if (tileId > 0)
                        {
                            var tile = tileSet.Tiles[tileId];
                            Texture tex;

                            if (!_tileTextures.ContainsKey(tile.Image.Source))
                            {
                                tex = new Texture(tile.Image.Width, tile.Image.Height);
                                tex.LoadImage(new AssetItem($"./resources/data/textures/{tile.Image.Source}"));

                                _tileTextures.Add(tile.Image.Source, tex);
                            }
                            else
                            {
                                tex = _tileTextures[tile.Image.Source];
                            }

                            var sprite = new Sprite()
                            {
                                Width = tile.Image.Width,
                                Height = tile.Image.Height,
                                Position = new Vector2(
                                    (x * tileWidth / 2) + (y * tileWidth / 2),
                                    (y * tileHeight / 2) - (x * tileHeight / 2)
                                ),
                                Texture = tex,
                                Sorting = i,
                            };
                            
                            MainLayer.Add(sprite);
                        }

                        i++;
                    }

                    yield return true;
                }
                
                Console.WriteLine($"Total: {i}, Expected: {mapWidth * mapHeight}");
            }
        }

        protected override void DeInitialize()
        {
            Camera.SetController(null);
        }
    }
}