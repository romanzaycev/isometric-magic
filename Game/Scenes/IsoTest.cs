using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using IsometricMagic.Engine;
using IsometricMagic.Game.Character;
using IsometricMagic.Game.Controllers.Camera;
using IsometricMagic.Game.Model;
using SDL2;

namespace IsometricMagic.Game.Scenes
{
    public class IsoTest : Scene
    {
        private readonly Dictionary<string, Texture> _tileTextures = new();
        private IsoWorldPositionConverter _positionConverter;
        private Human _human;
        private LookAtController _camController = new();

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
                                Position = _positionConverter.GetCanvasPosition(x * tileWidth / 2, y * tileWidth / 2),
                                Texture = tex,
                                Sorting = i,
                                OriginPoint = OriginPoint.LeftBottom,
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
            var moveX = 0;
            var moveY = 0;
            const int maxAbsMove = 5;
            
            if (Input.IsPressed(SDL.SDL_Keycode.SDLK_UP))
            {
                moveY = -5;
            }

            if (Input.IsPressed(SDL.SDL_Keycode.SDLK_DOWN))
            {
                moveY = 5;
            }
            
            if (Input.IsPressed(SDL.SDL_Keycode.SDLK_LEFT))
            {
                moveX = -5;
            }
            
            if (Input.IsPressed(SDL.SDL_Keycode.SDLK_RIGHT))
            {
                moveX = 5;
            }

            var absMoveX = Math.Abs(moveX);
            var absMoveY = Math.Abs(moveY);
            
            if (Math.Abs(moveX) > 0)
            {
                moveX = (moveX < 0) ? -Math.Min(absMoveX, maxAbsMove) : Math.Min(absMoveX, maxAbsMove);
            }
            
            if (Math.Abs(moveY) > 0)
            {
                moveY = (moveY < 0) ? -Math.Min(absMoveY, maxAbsMove) : Math.Min(absMoveY, maxAbsMove);
            }

            const int worldBorderThreshold = 30;
            var nextXPos = _human.WorldPosX + moveX;
            var nextYPos = _human.WorldPosY + moveY;
            var isMoving = false;

            if (nextXPos >= worldBorderThreshold && nextXPos <= _positionConverter.WorldWidth - worldBorderThreshold)
            {
                if (_human.WorldPosX != nextXPos)
                {
                    _human.WorldPosX = nextXPos;
                    isMoving = true;
                    _human.Direction = GetDirection(moveX, moveY);
                }
            }
            
            if (nextYPos >= worldBorderThreshold && nextYPos <= _positionConverter.WorldHeight - worldBorderThreshold)
            {
                if (_human.WorldPosY != nextYPos)
                {
                    _human.WorldPosY = nextYPos;
                    isMoving = true;
                    _human.Direction = GetDirection(moveX, moveY);
                }
            }

            if (isMoving)
            {
                _human.State = HumanState.RUNNING;
            }
            else
            {
                _human.State = HumanState.IDLE;
            }

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

        private WorldDirection GetDirection(int moveX, int moveY)
        {
            if (moveX == 0 && moveY == 0)
            {
                return _human.Direction;
            }

            if (moveY < 0 && moveX == 0)
            {
                return WorldDirection.SW;
            }

            if (moveY > 0 && moveX == 0)
            {
                return WorldDirection.NE;
            }
            
            if (moveY == 0 && moveX < 0)
            {
                return WorldDirection.NW;
            }

            if (moveY == 0 && moveX > 0)
            {
                return WorldDirection.SE;
            }

            // --
            
            if (moveY > 0 && moveX > 0)
            {
                return WorldDirection.E;
            }
            
            if (moveY > 0 && moveX < 0)
            {
                return WorldDirection.N;
            }
            
            if (moveY < 0 && moveX > 0)
            {
                return WorldDirection.S;
            }
            
            return WorldDirection.W;
        }
    }
}