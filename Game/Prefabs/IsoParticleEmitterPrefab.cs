using System.Numerics;
using IsometricMagic.Game.Components.Particles;
using IsometricMagic.Game.Components.Spatial;
using IsometricMagic.Game.Model;

namespace IsometricMagic.Game.Prefabs
{
    public readonly record struct ParticleVisualSpec(
        string TexturePath,
        int Width,
        int Height,
        int SortingOffset = 0,
        OriginPoint OriginPoint = OriginPoint.Centered,
        string? NormalMapPath = null,
        Func<IMaterial?>? MaterialFactory = null
    );

    public sealed record IsoParticleEmitterPrefabSpec
    {
        public string EntityName { get; init; } = "IsoParticleEmitter";
        public IsoWorldPosition WorldPosition { get; init; } = new IsoWorldPosition(0f, 0f);
        public ParticleVisualSpec[] Visuals { get; init; } = Array.Empty<ParticleVisualSpec>();

        public int Capacity { get; init; } = 256;
        public float RatePerSecond { get; init; }
        public ParticleSpawnShape SpawnShape { get; init; } = ParticleSpawnShape.Point;
        public float SpawnRadius { get; init; }
        public Vector2 SpawnBoxSize { get; init; } = Vector2.Zero;
        public Vector2 Offset { get; init; } = Vector2.Zero;
        public bool UseEntityTransform { get; init; } = false;
        public bool Emitting { get; init; } = true;

        public float LifetimeMin { get; init; } = 0.5f;
        public float LifetimeMax { get; init; } = 1f;
        public Vector2 VelocityMin { get; init; } = Vector2.Zero;
        public Vector2 VelocityMax { get; init; } = Vector2.Zero;
        public Vector2 Gravity { get; init; } = Vector2.Zero;
        public float Drag { get; init; }
        public float SizeMin { get; init; } = 1f;
        public float SizeMax { get; init; } = 1f;
        public float RotationMin { get; init; }
        public float RotationMax { get; init; }
        public float AngularVelocityMin { get; init; }
        public float AngularVelocityMax { get; init; }

        public int BaseSorting { get; init; }
        public int LayerBase { get; init; }
        public int Bias { get; init; }

        public ColorGradient.Key[] ColorOverLifeKeys { get; init; } = Array.Empty<ColorGradient.Key>();
        public FloatCurve.Key[] SizeOverLifeKeys { get; init; } = Array.Empty<FloatCurve.Key>();
    }

    public sealed class IsoParticleEmitterPrefab
    {
        private readonly IsoParticleEmitterPrefabSpec _spec;

        public IsoParticleEmitterPrefab(IsoParticleEmitterPrefabSpec spec)
        {
            _spec = spec;
        }

        public Entity Instantiate(Scene scene, Entity? parent = null)
        {
            var entity = scene.CreateEntity(_spec.EntityName, parent);
            entity.AddComponent(new IsoWorldPositionComponent
            {
                Position = _spec.WorldPosition
            });

            entity.AddComponent(new CanvasPositionComponent());

            entity.AddComponent(new IsoWorldToCanvasPositionSyncComponent());

            var lease = new SharedTextureLeaseComponent();
            var visuals = new ParticleVisual[_spec.Visuals.Length];

            for (var i = 0; i < _spec.Visuals.Length; i++)
            {
                var visualSpec = _spec.Visuals[i];
                var texture = Texture.AcquireShared(visualSpec.TexturePath, visualSpec.Width, visualSpec.Height);
                lease.Add(texture);

                Texture? normalMap = null;
                if (!string.IsNullOrWhiteSpace(visualSpec.NormalMapPath))
                {
                    normalMap = Texture.AcquireShared(
                        visualSpec.NormalMapPath!,
                        visualSpec.Width,
                        visualSpec.Height
                    );
                    lease.Add(normalMap);
                }

                visuals[i] = new ParticleVisual(texture, visualSpec.Width, visualSpec.Height)
                {
                    SortingOffset = visualSpec.SortingOffset,
                    OriginPoint = visualSpec.OriginPoint,
                    Material = visualSpec.MaterialFactory?.Invoke(),
                    NormalMap = normalMap
                };
            }

            entity.AddComponent(lease);

            var particles = new ParticleSystemComponent
            {
                TargetLayer = scene.MainLayer,
                Visuals = visuals,
                Capacity = _spec.Capacity,
                RatePerSecond = _spec.RatePerSecond,
                SpawnShape = _spec.SpawnShape,
                SpawnRadius = _spec.SpawnRadius,
                SpawnBoxSize = _spec.SpawnBoxSize,
                Offset = _spec.Offset,
                UseEntityTransform = _spec.UseEntityTransform,
                Emitting = _spec.Emitting,
                LifetimeMin = _spec.LifetimeMin,
                LifetimeMax = _spec.LifetimeMax,
                VelocityMin = _spec.VelocityMin,
                VelocityMax = _spec.VelocityMax,
                Gravity = _spec.Gravity,
                Drag = _spec.Drag,
                SizeMin = _spec.SizeMin,
                SizeMax = _spec.SizeMax,
                RotationMin = _spec.RotationMin,
                RotationMax = _spec.RotationMax,
                AngularVelocityMin = _spec.AngularVelocityMin,
                AngularVelocityMax = _spec.AngularVelocityMax,
                BaseSorting = _spec.BaseSorting
            };

            if (_spec.ColorOverLifeKeys.Length > 0)
            {
                particles.ColorOverLife.SetKeys(_spec.ColorOverLifeKeys);
            }

            if (_spec.SizeOverLifeKeys.Length > 0)
            {
                particles.SizeOverLife.SetKeys(_spec.SizeOverLifeKeys);
            }

            entity.AddComponent(particles);

            entity.AddComponent(new IsoParticleDepthSortComponent
            {
                LayerBase = _spec.LayerBase,
                Bias = _spec.Bias
            });

            return entity;
        }
    }
}
