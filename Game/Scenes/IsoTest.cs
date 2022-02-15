using System.Collections;
using System.Collections.Generic;
using System.Linq;
using IsometricMagic.Engine;
using IsometricMagic.Game.Character;
using IsometricMagic.Game.Controllers.Camera;
using IsometricMagic.Game.Controllers.Character;
using IsometricMagic.Game.Model;

namespace IsometricMagic.Game.Scenes
{
    public class IsoTest : Scene
    {
        private readonly Dictionary<string, Texture> _tileTextures = new();
        private IsoWorldPositionConverter _positionConverter;
        private Human _human;
        private readonly LookAtController _camController = new();
        private readonly CharacterMovementController _movementController = new Mouse();
        //private readonly CharacterMovementController _movementController = new Keyboard();

        public IsoTest() : base("iso_test", true)
        {
        }

        protected override IEnumerator InitializeAsync()
        {
            // Camera setup
            Camera.SetController(_camController);
            
            // Map setup
            var map = Maps.Loader.Load("map1");
            yield return true;

            var tileSet = Tiles.Loader.Load(map.TileSet);
            yield return true;

            if (map != null)
            {
                var mainLayer = map.Layers.First(l => l.Name == "main");

                var mapWidth = map.Width;
                var mapHeight = map.Height;

                var tileWidth = map.TileWidth;
                var tileHeight = map.TileHeight;

                _positionConverter = new IsoWorldPositionConverter(
                    tileWidth,
                    tileHeight,
                    mapWidth,
                    mapHeight
                );
                
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
                                tex.LoadImage($"./resources/data/textures/{tile.Image.Source}");

                                _tileTextures.Add(tile.Image.Source, tex);
                            }
                            else
                            {
                                tex = _tileTextures[tile.Image.Source];
                            }

                            var sprite = new Sprite
                            {
                                Width = tile.Image.Width,
                                Height = tile.Image.Height,
                                Position = _positionConverter.GetTilePosition(x, y),
                                Texture = tex,
                                Sorting = i,
                                OriginPoint = OriginPoint.LeftBottom
                            };
                            
                            MainLayer.Add(sprite);
                        }

                        i++;
                    }

                    yield return true;
                }

                _human = new Human(MainLayer)
                {
                    WorldPosY = 400,
                    WorldPosX = 400
                };
            }
        }

        public override void Update()
        {
            _movementController.HandleMovement(_human, _positionConverter);
            _human.CurrentSequence?.Update(Application.DeltaTime);
            
            if (_human.CurrentSequence?.CurrentSprite != null)
            {
                var pos = _positionConverter.GetCanvasPosition(_human);

                _human.CurrentSequence.CurrentSprite.Position = pos;
                _camController.SetPos(pos);
            }
        }

        protected override void DeInitialize()
        {
            Camera.SetController(null);
        }
    }
}