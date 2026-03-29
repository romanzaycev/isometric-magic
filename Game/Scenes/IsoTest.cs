using System.Collections;
using System.Numerics;
using IsometricMagic.Engine;
using IsometricMagic.Engine.Graphics.Effects;
using IsometricMagic.Engine.Graphics.Lighting;
using IsometricMagic.Game.Components.Actor;
using IsometricMagic.Game.Components.Camera;
using IsometricMagic.Game.Components.Spatial;
using IsometricMagic.Game.Components.Character.Humanoid;
using IsometricMagic.Game.Components.Tilemap;
using IsometricMagic.Game.Components.Vfx.Light;
using IsometricMagic.Game.Controllers.Character;
using IsometricMagic.Game.Model;

namespace IsometricMagic.Game.Scenes
{
    public class IsoTest : Scene
    {
        public IsoTest() : base("iso_test", true)
        {
        }

        protected override IEnumerator InitializeAsync()
        {
            // Tilemap
            var map = Maps.Loader.Load("map1");
            yield return true;

            var tileSet = Tiles.Loader.Load(map.TileSet);
            yield return true;

            var positionConverter = new IsoWorldPositionConverter(
                map.TileWidth,
                map.TileHeight,
                map.Width,
                map.Height
            );

            var mapEntity = CreateEntity("Map");
            var tilemapRenderer = mapEntity.AddComponent<IsoTilemapRendererComponent>();
            tilemapRenderer.Load(map, tileSet, positionConverter, MainLayer);
            tilemapRenderer.BuildAll();

            // Player
            var playerEntity = CreateEntity("Player");
            var worldPosComp = playerEntity.AddComponent<WorldPositionComponent>();
            worldPosComp.WorldPosX = 400;
            worldPosComp.WorldPosY = 400;

            var motor = new MotorComponent();
            motor.SetConverter(positionConverter);
            playerEntity.AddComponent(motor);
            
            var playerAnimationComponent = new HumanoidAnimationComponent
            {
                TargetLayer = MainLayer,
                Sorting = 1000
            };
            playerEntity.AddComponent(playerAnimationComponent);
            // Player movement controller
            playerEntity.AddComponent(new KeyboardOrGamepad());

            var cameraFollow = new CameraFollowComponent();
            cameraFollow.SetConverter(positionConverter);
            playerEntity.AddComponent(cameraFollow);

            var positionSync = new HumanoidWorldPositionSyncComponent();
            positionSync.SetConverter(positionConverter);
            playerEntity.AddComponent(positionSync);

            // Vfx
            var lightCenter = positionConverter.GetCanvasPosition(new Vector2(600, 600));

            var lightEntity = CreateEntity("MovingLight");
            lightEntity.AddComponent(new WorldPositionComponent());
            var orbitLight = new OrbitLightComponent
            {
                Center = lightCenter,
                Radius = 300f,
                Speed = 0.8f
            };
            lightEntity.AddComponent(orbitLight);
            lightEntity.AddComponent(new FireCircleComponent()
            {
                TargetLayer = MainLayer,
                Sorting = 1100,
            });
            
            // Lighting
            Lighting.AmbientIntensity = 0.5f;
            Lighting.Add(
                new Light2D(positionConverter.GetCanvasPosition(new Vector2(410, 410)))
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
            
            // pp
            PostProcess.Add(new BloomEffect
            {
                Threshold = 1.2f,
                Knee = 0.6f,
                Intensity = 0.9f,
                BlurIterations = 4
            });
            PostProcess.Add(new ToneMapEffect
            {
                Exposure = 0.55f,
                Gamma = 1.2f,
                Contrast = 1.05f,
            });
            PostProcess.Add(new VignetteEffect
            {
                Intensity = 0.2f
            });
        }

        public override void Update()
        {
        }

        protected override void DeInitialize()
        {
        }
    }
}
