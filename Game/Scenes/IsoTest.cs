using System.Collections;
using System.Numerics;
using IsometricMagic.Engine;
using IsometricMagic.Engine.Graphics.Effects;
using IsometricMagic.Engine.Graphics.Lighting;
using IsometricMagic.Game.Components;
using IsometricMagic.Game.Model;

namespace IsometricMagic.Game.Scenes
{
    public class IsoTest : Scene
    {
        private IsoWorldPositionConverter _positionConverter = null!;
        private Entity? _playerEntity;
        private Entity? _lightEntity;
        private HumanoidAnimationComponent? _animationComponent;

        public IsoTest() : base("iso_test", true)
        {
        }

        protected override IEnumerator InitializeAsync()
        {
            var map = Maps.Loader.Load("map1");
            yield return true;

            var tileSet = Tiles.Loader.Load(map.TileSet);
            yield return true;

            _positionConverter = new IsoWorldPositionConverter(
                map.TileWidth,
                map.TileHeight,
                map.Width,
                map.Height
            );

            var mapEntity = CreateEntity("Map");
            var tilemapRenderer = mapEntity.AddComponent<IsoTilemapRendererComponent>();
            tilemapRenderer.Load(map, tileSet, _positionConverter, MainLayer);
            tilemapRenderer.BuildAll();

            _playerEntity = CreateEntity("Player");
            var worldPosComp = _playerEntity.AddComponent<WorldPositionComponent>();
            worldPosComp.WorldPosX = 400;
            worldPosComp.WorldPosY = 400;

            _animationComponent = new HumanoidAnimationComponent
            {
                TargetLayer = MainLayer,
                Sorting = 1000
            };
            _playerEntity.AddComponent(_animationComponent);

            var motor = new HumanoidMotorComponent();
            motor.SetConverter(_positionConverter);
            _playerEntity.AddComponent(motor);

            var cameraFollow = new CameraFollowComponent();
            cameraFollow.SetConverter(_positionConverter);
            _playerEntity.AddComponent(cameraFollow);

            var positionSync = new HumanoidWorldPositionSyncComponent();
            positionSync.SetConverter(_positionConverter);
            _playerEntity.AddComponent(positionSync);

            var lightCenter = _positionConverter.GetCanvasPosition(new Vector2(600, 600));

            _lightEntity = CreateEntity("MovingLight");
            var orbitLight = new OrbitLightComponent
            {
                Center = lightCenter,
                Radius = 300f,
                Speed = 0.8f
            };
            _lightEntity.AddComponent(orbitLight);

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
        }

        public override void Update()
        {
        }

        protected override void DeInitialize()
        {
        }
    }
}
