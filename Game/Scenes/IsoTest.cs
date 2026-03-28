using System.Collections;
using System.Numerics;
using IsometricMagic.Engine;
using IsometricMagic.Engine.Graphics.Effects;
using IsometricMagic.Engine.Graphics.Lighting;
using IsometricMagic.Engine.Graphics.Materials;
using IsometricMagic.Game.Character;
using IsometricMagic.Game.Controllers.Camera;
using IsometricMagic.Game.Controllers.Character;
using IsometricMagic.Game.Model;

namespace IsometricMagic.Game.Scenes
{
    public class IsoTest : Scene
    {
        private readonly Dictionary<string, Texture> _tileTextures = new();
        private IsoWorldPositionConverter _positionConverter = null!;
        private Human _human = null!;
        private int _mapWidth;
        private int _mapHeight;
        private int _tileWidth;
        private int _tileHeight;
        private readonly LookAtController _camController = new();
        private readonly CharacterMovementController _movementController = new KeyboardOrGamepad();
        private Light2D _movingLight = null!;
        private float _lightAngle;
        private Vector2 _lightCenter;

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

                _mapWidth = map.Width;
                _mapHeight = map.Height;

                _tileWidth = map.TileWidth;
                _tileHeight = map.TileHeight;

                _positionConverter = new IsoWorldPositionConverter(
                    _tileWidth,
                    _tileHeight,
                    _mapWidth,
                    _mapHeight
                );
                
                var i = 0;

                for (var y = 0; y < _mapHeight; y++)
                {
                    for (var x = _mapWidth - 1; x >= 0; x--)
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
                            sprite.Material = new NormalMappedLitSpriteMaterial();
                            
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

            PostProcess.Add(new VignetteEffect { Intensity = 0.2f });
            Lighting.AmbientIntensity = 0.5f;
            Lighting.Add(
                new Light2D(_positionConverter.GetCanvasPosition(new Vector2(410, 410)))
                {
                    Intensity = 2f,
                    Radius = 512f,
                    Height = 1.8f,
                    Falloff = 2f,
                    InnerRadius = 64f,
                    CenterAttenuation = 0.1f,
                    Color = new Vector3(0.1f, 1f, 1f),
                }
            );

            _lightCenter = _positionConverter.GetCanvasPosition(
                new Vector2(600, 600)
            );
            _movingLight = new Light2D(_lightCenter)
            {
                Intensity = 2.5f,
                Radius = 256f,
                Height = 2f,
                Falloff = 2f,
                InnerRadius = 32f,
                CenterAttenuation = 0.5f,
                Color = new Vector3(1f, 0.4f, 0.1f),
            };
            Lighting.Add(_movingLight);
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

            _lightAngle += Application.DeltaTime * 0.8f;
            var offset = new Vector2(MathF.Cos(_lightAngle) * 300f, MathF.Sin(_lightAngle) * 300f);
            _movingLight.Position = _lightCenter + offset;
        }

        protected override void DeInitialize()
        {
            Camera.SetController(null);
        }
    }
}
