using System.Collections;
using System.Numerics;
using IsometricMagic.Engine;
using IsometricMagic.Engine.Graphics.Effects;
using IsometricMagic.Engine.Graphics.Lighting;
using IsometricMagic.Engine.Graphics.Materials;
using IsometricMagic.Engine.Particles;
using IsometricMagic.Game.Components.Actor;
using IsometricMagic.Game.Components.Camera;
using IsometricMagic.Game.Components.Spatial;
using IsometricMagic.Game.Components.Character.Humanoid;
using IsometricMagic.Game.Components.Rendering;
using IsometricMagic.Game.Components.Tilemap;
using IsometricMagic.Game.Components.Vfx.Light;
using IsometricMagic.Game.Components.Collision;
using IsometricMagic.Game.Components.Particles;
using IsometricMagic.Game.Controllers.Character;
using IsometricMagic.Game.Model;
using IsometricMagic.Game.Rendering;

namespace IsometricMagic.Game.Scenes
{
    public class IsoTest : Scene
    {
        public IsoTest() : base("iso_test", true)
        {
        }

        protected override IEnumerator InitializeAsync()
        {
            # region Tilemap
            
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
            var worldLayerBase = tilemapRenderer.WorldLayerBase;

            var collisionWorld = CreateEntity("CollisionWorld");
            collisionWorld.AddComponent<CollisionWorldComponent>();
            
            # endregion

            # region Player
            var playerEntity = CreateEntity("Player");
            var worldPosComp = playerEntity.AddComponent<WorldPositionComponent>();
            worldPosComp.WorldPosX = 470;
            worldPosComp.WorldPosY = 470;

            playerEntity.AddComponent(new WorldColliderComponent
            {
                Radius = 20f,
                IsStatic = false,
                // DebugDraw = true,
            });
            
            var motor = new MotorComponent();
            motor.SetConverter(positionConverter);
            playerEntity.AddComponent(motor);
            
            var playerAnimationComponent = new HumanoidAnimationComponent
            {
                TargetLayer = MainLayer,
                Sorting = 0
            };
            playerEntity.AddComponent(playerAnimationComponent);
            // Player movement controller
            playerEntity.AddComponent(new KeyboardOrGamepad());

            var cameraFollow = new CameraFollowComponent();
            cameraFollow.SetConverter(positionConverter);
            playerEntity.AddComponent(cameraFollow);

            var positionSync = new HumanoidWorldPositionSyncComponent();
            positionSync.SetConverter(positionConverter);
            positionSync.LayerBase = worldLayerBase;
            playerEntity.AddComponent(positionSync);
            
            # endregion

            # region Objects
            
            var stone0 = CreateEntity("stone0");
            stone0.AddComponent(new WorldPositionComponent()
            {
                WorldPosX = 410,
                WorldPosY = 410,
            });
            var stoneEmissionMaterial = new EmissiveNormalMappedLitSpriteMaterial()
            {
                EmissionColor = new Vector3(0.37f, 0.81f, 0.51f),
                EmissionIntensity = 3f,
                EmissionMapPath = "./resources/data/textures/stone0_em.png",
            };
            stone0.AddComponent(new SpriteRendererComponent()
            {
                ImagePath = "./resources/data/textures/stone0.png",
                Width = 256,
                Height = 256,
                OriginPoint = OriginPoint.BottomCenter,
                PositionMode = SpritePositionMode.IsoWorldFromWorldPositionComponent,
                Sorting = 0,
                Material = stoneEmissionMaterial,
            });
            stone0.AddComponent(new IsoDepthSortComponent()
            {
                Bias = IsoSort.BiasObject,
                LayerBase = worldLayerBase
            });
            stone0.AddComponent(new WorldColliderComponent
            {
                Radius = 60f,
                IsStatic = true,
                // DebugDraw = true
            });

            var stoneCanvasPos = positionConverter.GetCanvasPosition(new Vector2(410, 410));
            var emissionColor = stoneEmissionMaterial.EmissionColor;

            var sparkTexture1 = new Texture(512, 512);
            sparkTexture1.LoadImage("./resources/data/textures/vfx/particles/star_01.png");
            var sparkTexture2 = new Texture(512, 512);
            sparkTexture2.LoadImage("./resources/data/textures/vfx/particles/star_02.png");
            var sparkTexture3 = new Texture(512, 512);
            sparkTexture3.LoadImage("./resources/data/textures/vfx/particles/star_03.png");

            var sparkMaterial = new EmissiveNormalMappedLitSpriteMaterial
            {
                EmissionColor = emissionColor,
                EmissionIntensity = 2.2f
            };

            var sparkVisuals = new[]
            {
                new ParticleVisual(sparkTexture1, 512, 512) { Material = sparkMaterial },
                new ParticleVisual(sparkTexture2, 512, 512) { Material = sparkMaterial },
                new ParticleVisual(sparkTexture3, 512, 512) { Material = sparkMaterial }
            };

            var sparksEntity = CreateEntity("Stone0Sparks");
            sparksEntity.AddComponent(new WorldPositionComponent
            {
                WorldPosX = 410,
                WorldPosY = 410,
            });
            var sparks = new ParticleSystemComponent
            {
                TargetLayer = MainLayer,
                Visuals = sparkVisuals,
                Capacity = 160,
                RatePerSecond = 10f,
                SpawnShape = ParticleSpawnShape.Circle,
                SpawnRadius = 20f,
                Position = stoneCanvasPos,
                Offset = new Vector2(0f, -220f),
                UseEntityTransform = false,
                LifetimeMin = 0.9f,
                LifetimeMax = 2.5f,
                VelocityMin = new Vector2(-18f, -70f),
                VelocityMax = new Vector2(18f, -150f),
                Gravity = new Vector2(0f, -10f),
                Drag = 0.35f,
                SizeMin = 0.07f,
                SizeMax = 0.12f,
                RotationMin = 0f,
                RotationMax = 1f,
                AngularVelocityMin = -0.6f,
                AngularVelocityMax = 0.6f,
                BaseSorting = 1200
            };
            sparks.ColorOverLife.SetKeys(
                new ColorGradient.Key(0f, new Vector4(emissionColor, 0f)),
                new ColorGradient.Key(0.15f, new Vector4(emissionColor, 1f)),
                new ColorGradient.Key(1f, new Vector4(emissionColor, 0f))
            );
            sparks.SizeOverLife.SetKeys(
                new FloatCurve.Key(0f, 0.5f),
                new FloatCurve.Key(0.2f, 1f),
                new FloatCurve.Key(1f, 0f)
            );
            sparksEntity.AddComponent(sparks);
            sparksEntity.AddComponent(new IsoParticleEmitterComponent
            {
                LayerBase = worldLayerBase
            });

            var smokeTexture1 = new Texture(512, 512);
            smokeTexture1.LoadImage("./resources/data/textures/vfx/particles/smoke_01.png");
            var smokeTexture2 = new Texture(512, 512);
            smokeTexture2.LoadImage("./resources/data/textures/vfx/particles/smoke_02.png");
            var smokeTexture3 = new Texture(512, 512);
            smokeTexture3.LoadImage("./resources/data/textures/vfx/particles/smoke_03.png");

            var smokeMaterial = new EmissiveNormalMappedLitSpriteMaterial
            {
                EmissionColor = new Vector3(0.49f, 0.91f, 0.63f),
                EmissionIntensity = 1.2f
            };

            var smokeVisuals = new[]
            {
                new ParticleVisual(smokeTexture1, 512, 512) { Material = smokeMaterial },
                new ParticleVisual(smokeTexture2, 512, 512) { Material = smokeMaterial },
                new ParticleVisual(smokeTexture3, 512, 512) { Material = smokeMaterial }
            };

            var smokeEntity = CreateEntity("Stone0Smoke");
            smokeEntity.AddComponent(new WorldPositionComponent
            {
                WorldPosX = 410,
                WorldPosY = 410,
            });
            var smoke = new ParticleSystemComponent
            {
                TargetLayer = MainLayer,
                Visuals = smokeVisuals,
                Capacity = 96,
                RatePerSecond = 3f,
                SpawnShape = ParticleSpawnShape.Circle,
                SpawnRadius = 18f,
                Position = stoneCanvasPos,
                Offset = new Vector2(0f, -200f),
                UseEntityTransform = false,
                LifetimeMin = 2.2f,
                LifetimeMax = 3.6f,
                VelocityMin = new Vector2(-8f, -20f),
                VelocityMax = new Vector2(8f, -45f),
                Gravity = new Vector2(0f, -6f),
                Drag = 0.2f,
                SizeMin = 0.22f,
                SizeMax = 0.35f,
                RotationMin = 0f,
                RotationMax = 1f,
                AngularVelocityMin = -0.1f,
                AngularVelocityMax = 0.1f,
                BaseSorting = 1150
            };
            smoke.ColorOverLife.SetKeys(
                new ColorGradient.Key(0f, new Vector4(emissionColor, 0f)),
                new ColorGradient.Key(0.2f, new Vector4(emissionColor, 0.35f)),
                new ColorGradient.Key(1f, new Vector4(emissionColor, 0f))
            );
            smoke.SizeOverLife.SetKeys(
                new FloatCurve.Key(0f, 0.6f),
                new FloatCurve.Key(0.6f, 1f),
                new FloatCurve.Key(1f, 1.2f)
            );
            smokeEntity.AddComponent(smoke);
            smokeEntity.AddComponent(new IsoParticleEmitterComponent
            {
                LayerBase = worldLayerBase
            });
            # endregion
            
            # region Vfx
            
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
                LayerBase = worldLayerBase + tilemapRenderer.LayerStride,
                Bias = IsoSort.BiasVfx,
            });
            
            # endregion
            
            # region Lighting
            
            Lighting.AmbientIntensity = 0.45f;
            Lighting.Add(
                new Light2D(positionConverter.GetCanvasPosition(new Vector2(410, 410)))
                {
                    Intensity = 5f,
                    Radius = 512f,
                    Height = 2f,
                    Falloff = 2f,
                    InnerRadius = 64f,
                    CenterAttenuation = 0.1f,
                    Color = new Vector3(0.37f, 0.81f, 0.51f),
                }
            );
            
            # endregion
            
            # region pp
            
            PostProcess.Add(new BloomEffect
            {
                Threshold = 1.2f,
                Knee = 0.6f,
                Intensity = 0.95f,
                BlurIterations = 4
            });
            PostProcess.Add(new ToneMapEffect
            {
                Exposure = 0.6f,
                Gamma = 1.3f,
                Contrast = 1.05f,
            });
            PostProcess.Add(new VignetteEffect
            {
                Intensity = 0.2f
            });
            
            #endregion
        }

        public override void Update()
        {
        }

        protected override void DeInitialize()
        {
        }
    }
}
