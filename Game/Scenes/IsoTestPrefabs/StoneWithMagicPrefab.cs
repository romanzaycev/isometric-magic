using System.Numerics;
using IsometricMagic.Game.Components.Collision;
using IsometricMagic.Game.Components.Rendering;
using IsometricMagic.Game.Components.Spatial;
using IsometricMagic.Game.Components.Vfx.Light;
using IsometricMagic.Game.Model;
using IsometricMagic.Game.Prefabs;
using IsometricMagic.Game.Rendering;

namespace IsometricMagic.Game.Scenes.IsoTestPrefabs
{
    internal readonly record struct StoneWithMagicPrefabSpec(
        string StoneEntityName,
        IsoWorldPosition WorldPosition,
        int WorldLayerBase,
        float ColliderRadius = 60f,
        Vector3 EmissionColor = default,
        float StoneEmissionIntensity = 3f,
        float SparkEmissionIntensity = 2.2f,
        float SmokeEmissionIntensity = 1.2f
    );

    internal sealed class StoneWithMagicPrefab
    {
        private static readonly IsoParticleEmitterPrefabSpec BaseSparksPrototype = new()
        {
            EntityName = "StoneSparks",
            Capacity = 160,
            RatePerSecond = 10f,
            SpawnShape = ParticleSpawnShape.Circle,
            SpawnRadius = 20f,
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
            BaseSorting = 1200,
            Bias = IsoSort.BiasVfx
        };

        private static readonly IsoParticleEmitterPrefabSpec BaseSmokePrototype = new()
        {
            EntityName = "StoneSmoke",
            Capacity = 96,
            RatePerSecond = 3f,
            SpawnShape = ParticleSpawnShape.Circle,
            SpawnRadius = 18f,
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
            BaseSorting = 1150,
            Bias = IsoSort.BiasVfx
        };

        private readonly StoneWithMagicPrefabSpec _spec;

        public StoneWithMagicPrefab(StoneWithMagicPrefabSpec spec)
        {
            _spec = spec;
        }

        public Entity Instantiate(Scene scene, Entity? parent = null)
        {
            var emissionColor = _spec.EmissionColor == default ? new Vector3(0.37f, 0.81f, 0.51f) : _spec.EmissionColor;

            var stone = scene.CreateEntity(_spec.StoneEntityName, parent);
            stone.AddComponent(new IsoWorldPositionComponent
            {
                Position = _spec.WorldPosition
            });
            stone.AddComponent(new IsoWorldToCanvasPositionSyncComponent());

            var stoneMaterial = new EmissiveNormalMappedLitSpriteMaterial
            {
                EmissionColor = emissionColor,
                EmissionIntensity = _spec.StoneEmissionIntensity,
                EmissionMapPath = "./resources/data/textures/stone0_em.png"
            };

            stone.AddComponent(new SpriteRendererComponent
            {
                ImagePath = "./resources/data/textures/stone0.png",
                Width = 256,
                Height = 256,
                OriginPoint = OriginPoint.BottomCenter,
                Sorting = 0,
                Material = stoneMaterial,
                TargetLayer = scene.MainLayer
            });

            stone.AddComponent(new IsoDepthSortComponent
            {
                Bias = IsoSort.BiasObject,
                LayerBase = _spec.WorldLayerBase
            });

            stone.AddComponent(new WorldColliderComponent
            {
                Radius = _spec.ColliderRadius,
                IsStatic = true,
                Offset = new Vector2(55, -55),
            });
            
            stone.AddComponent(new Light2DComponent()
            {
                Intensity = 5f,
                Radius = 512f,
                Height = 2f,
                Falloff = 2f,
                InnerRadius = 64f,
                CenterAttenuation = 0.1f,
                Color = new Vector3(0.37f, 0.81f, 0.51f),
            });

            var sparksSpec = BaseSparksPrototype with
            {
                EntityName = _spec.StoneEntityName + "Sparks",
                WorldPosition = _spec.WorldPosition,
                LayerBase = _spec.WorldLayerBase,
                Visuals = CreateSparkVisuals(emissionColor, _spec.SparkEmissionIntensity),
                ColorOverLifeKeys = new[]
                {
                    new ColorGradient.Key(0f, new Vector4(emissionColor, 0f)),
                    new ColorGradient.Key(0.15f, new Vector4(emissionColor, 1f)),
                    new ColorGradient.Key(1f, new Vector4(emissionColor, 0f))
                },
                SizeOverLifeKeys = new[]
                {
                    new FloatCurve.Key(0f, 0.5f),
                    new FloatCurve.Key(0.2f, 1f),
                    new FloatCurve.Key(1f, 0f)
                }
            };
            new IsoParticleEmitterPrefab(sparksSpec).Instantiate(scene, parent);

            var smokeSpec = BaseSmokePrototype with
            {
                EntityName = _spec.StoneEntityName + "Smoke",
                WorldPosition = _spec.WorldPosition,
                LayerBase = _spec.WorldLayerBase,
                Visuals = CreateSmokeVisuals(emissionColor, _spec.SmokeEmissionIntensity),
                ColorOverLifeKeys = new[]
                {
                    new ColorGradient.Key(0f, new Vector4(emissionColor, 0f)),
                    new ColorGradient.Key(0.2f, new Vector4(emissionColor, 0.35f)),
                    new ColorGradient.Key(1f, new Vector4(emissionColor, 0f))
                },
                SizeOverLifeKeys = new[]
                {
                    new FloatCurve.Key(0f, 0.6f),
                    new FloatCurve.Key(0.6f, 1f),
                    new FloatCurve.Key(1f, 1.2f)
                }
            };
            new IsoParticleEmitterPrefab(smokeSpec).Instantiate(scene, parent);

            return stone;
        }

        private static ParticleVisualSpec[] CreateSparkVisuals(Vector3 color, float intensity)
        {
            return new[]
            {
                new ParticleVisualSpec("./resources/data/textures/vfx/particles/star_01.png", 512, 512,
                    MaterialFactory: () => new EmissiveNormalMappedLitSpriteMaterial
                    {
                        EmissionColor = color,
                        EmissionIntensity = intensity
                    }),
                new ParticleVisualSpec("./resources/data/textures/vfx/particles/star_02.png", 512, 512,
                    MaterialFactory: () => new EmissiveNormalMappedLitSpriteMaterial
                    {
                        EmissionColor = color,
                        EmissionIntensity = intensity
                    }),
                new ParticleVisualSpec("./resources/data/textures/vfx/particles/star_03.png", 512, 512,
                    MaterialFactory: () => new EmissiveNormalMappedLitSpriteMaterial
                    {
                        EmissionColor = color,
                        EmissionIntensity = intensity
                    })
            };
        }

        private static ParticleVisualSpec[] CreateSmokeVisuals(Vector3 color, float intensity)
        {
            return new[]
            {
                new ParticleVisualSpec("./resources/data/textures/vfx/particles/smoke_01.png", 512, 512,
                    MaterialFactory: () => new EmissiveNormalMappedLitSpriteMaterial
                    {
                        EmissionColor = color,
                        EmissionIntensity = intensity
                    }),
                new ParticleVisualSpec("./resources/data/textures/vfx/particles/smoke_02.png", 512, 512,
                    MaterialFactory: () => new EmissiveNormalMappedLitSpriteMaterial
                    {
                        EmissionColor = color,
                        EmissionIntensity = intensity
                    }),
                new ParticleVisualSpec("./resources/data/textures/vfx/particles/smoke_03.png", 512, 512,
                    MaterialFactory: () => new EmissiveNormalMappedLitSpriteMaterial
                    {
                        EmissionColor = color,
                        EmissionIntensity = intensity
                    })
            };
        }
    }
}
