using System;
using System.Numerics;
using IsometricMagic.Engine;

namespace IsometricMagic.Engine.Particles
{
    /// <summary>
    /// Sprite-based particle system that lives on a scene layer and updates a fixed pool of sprites.
    /// Uses LUT-based curves for color and size over lifetime to avoid per-frame allocations.
    /// </summary>
    /// <remarks>
    /// Typical setup (minimal):
    /// <code>
    /// var particles = entity.AddComponent<ParticleSystemComponent>();
    /// particles.TargetLayer = scene.MainLayer;
    /// particles.Visuals = new[]
    /// {
    ///     ParticleVisual.FromSprite(sparkSprite, sortingOffset: 2)
    /// };
    /// particles.Capacity = 256;
    /// particles.RatePerSecond = 40f;
    /// particles.LifetimeMin = 0.2f;
    /// particles.LifetimeMax = 0.6f;
    /// particles.VelocityMin = new Vector2(-30f, 10f);
    /// particles.VelocityMax = new Vector2(30f, 80f);
    /// particles.ColorOverLife.SetGradient(
    ///     new[] { (0f, new Vector4(1f, 1f, 1f, 1f)), (1f, new Vector4(1f, 1f, 1f, 0f)) }
    /// );
    /// </code>
    ///
    /// Emission control example (burst):
    /// <code>
    /// particles.Emitting = false; // stop continuous emission
    /// particles.Emit(20);         // emit one burst now
    /// </code>
    ///
    /// Coordinate space note:
    /// Particles are in canvas space. If <see cref="UseEntityTransform"/> is true,
    /// the emitter position is based on the owning entity's canvas position.
    /// </remarks>
    public sealed class ParticleSystemComponent : Component
    {
        private struct Particle
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public float Rotation;
            public float AngularVelocity;
            public float Age;
            public float Lifetime;
            public float Size;
            public Vector4 ColorScale;
            public int VisualIndex;
            public int SpriteIndex;
        }

        /// <summary>
        /// Layer that owns the particle sprites. Must be set before activation.
        /// If null on Awake, defaults to <see cref="Scene.MainLayer"/>.
        /// </summary>
        public SceneLayer? TargetLayer { get; set; }

        /// <summary>
        /// Visual variants for particles. Each spawned particle picks one at random.
        /// Controls texture/material/normal map and base size.
        /// </summary>
        public ParticleVisual[] Visuals { get; set; } = Array.Empty<ParticleVisual>();

        /// <summary>
        /// Maximum number of live particles. Also sizes the internal sprite pool.
        /// Higher values increase memory but avoid drops when emitting bursts.
        /// </summary>
        public int Capacity { get; set; } = 256;

        /// <summary>
        /// Enables continuous emission based on <see cref="RatePerSecond"/>.
        /// Set false to disable continuous emission while still allowing <see cref="Emit(int)"/>.
        /// </summary>
        public bool Emitting { get; set; } = true;

        /// <summary>
        /// Continuous emission rate. Particles per second when <see cref="Emitting"/> is true.
        /// </summary>
        public float RatePerSecond { get; set; }

        /// <summary>
        /// Spawn shape for the emitter. Affects how <see cref="SpawnRadius"/> and
        /// <see cref="SpawnBoxSize"/> are interpreted.
        /// </summary>
        public ParticleSpawnShape SpawnShape { get; set; } = ParticleSpawnShape.Point;

        /// <summary>
        /// Circle radius for <see cref="ParticleSpawnShape.Circle"/>.
        /// Ignored for other shapes.
        /// </summary>
        public float SpawnRadius { get; set; }

        /// <summary>
        /// Box size for <see cref="ParticleSpawnShape.Box"/> (full width/height).
        /// Ignored for other shapes.
        /// </summary>
        public Vector2 SpawnBoxSize { get; set; } = Vector2.Zero;

        /// <summary>
        /// Lifetime range in seconds. Each particle samples a random value in this range.
        /// Shorter lifetimes make bursts feel snappy; longer lifetimes create trails.
        /// </summary>
        public float LifetimeMin { get; set; } = 0.5f;

        /// <summary>
        /// Maximum lifetime in seconds. Must be >= <see cref="LifetimeMin"/>.
        /// </summary>
        public float LifetimeMax { get; set; } = 1.0f;

        /// <summary>
        /// Velocity range (X/Y). Each particle samples a random vector between min and max.
        /// Example: upward sparks: min=(-20, 30) max=(20, 120).
        /// </summary>
        public Vector2 VelocityMin { get; set; } = Vector2.Zero;

        /// <summary>
        /// Maximum velocity vector. Must be >= <see cref="VelocityMin"/> per component.
        /// </summary>
        public Vector2 VelocityMax { get; set; } = Vector2.Zero;

        /// <summary>
        /// Base size multiplier range. Applied before <see cref="SizeOverLife"/>.
        /// Example: size jitter: min=0.8, max=1.2.
        /// </summary>
        public float SizeMin { get; set; } = 1f;

        /// <summary>
        /// Maximum base size multiplier. Must be >= <see cref="SizeMin"/>.
        /// </summary>
        public float SizeMax { get; set; } = 1f;

        /// <summary>
        /// Initial rotation range in radians.
        /// </summary>
        public float RotationMin { get; set; }

        /// <summary>
        /// Maximum initial rotation in radians.
        /// </summary>
        public float RotationMax { get; set; }

        /// <summary>
        /// Angular velocity range in radians per second.
        /// Positive values follow <see cref="RotationClockwise"/>.
        /// </summary>
        public float AngularVelocityMin { get; set; }

        /// <summary>
        /// Maximum angular velocity in radians per second.
        /// </summary>
        public float AngularVelocityMax { get; set; }

        /// <summary>
        /// Constant acceleration applied every frame. Positive Y typically moves down.
        /// Use negative Y for rising smoke.
        /// </summary>
        public Vector2 Gravity { get; set; } = Vector2.Zero;

        /// <summary>
        /// Linear drag coefficient. 0 means no drag. Higher values damp velocity faster.
        /// </summary>
        public float Drag { get; set; }

        /// <summary>
        /// Random per-particle color scale range (RGBA). Multiplies <see cref="ColorOverLife"/>.
        /// Use this for subtle color variance or per-particle alpha jitter.
        /// </summary>
        public Vector4 ColorMin { get; set; } = new(1f, 1f, 1f, 1f);

        /// <summary>
        /// Maximum per-particle color scale.
        /// </summary>
        public Vector4 ColorMax { get; set; } = new(1f, 1f, 1f, 1f);

        /// <summary>
        /// Color (RGBA) over normalized lifetime [0..1]. Stored as a LUT for speed.
        /// Example: fade out: (0, 1,1,1,1) -> (1, 1,1,1,0).
        /// </summary>
        public ColorGradient ColorOverLife { get; } = new();

        /// <summary>
        /// Size multiplier over normalized lifetime [0..1]. Stored as a LUT for speed.
        /// Example: puff: (0,0.2) -> (0.3,1.0) -> (1,0.0).
        /// </summary>
        public FloatCurve SizeOverLife { get; } = new(1f);

        /// <summary>
        /// If true, the emitter position follows the owning entity's canvas position.
        /// If false, <see cref="Position"/> is used directly.
        /// </summary>
        public bool UseEntityTransform { get; set; } = true;

        /// <summary>
        /// Explicit emitter position in canvas space when <see cref="UseEntityTransform"/> is false.
        /// </summary>
        public Vector2 Position { get; set; } = Vector2.Zero;

        /// <summary>
        /// Additional offset added to the resolved emitter position.
        /// Useful for placing effects above/below a sprite.
        /// </summary>
        public Vector2 Offset { get; set; } = Vector2.Zero;

        /// <summary>
        /// Base sorting for all sprites in this system. Each visual can add an offset.
        /// Changing this does not re-sort existing sprites; it sets the value at spawn.
        /// </summary>
        public int BaseSorting { get; set; }

        /// <summary>
        /// Controls rotation direction semantics. True means positive angular velocity is clockwise.
        /// </summary>
        public bool RotationClockwise { get; set; } = true;

        private Particle[] _particles = Array.Empty<Particle>();
        private Sprite[] _sprites = Array.Empty<Sprite>();
        private int[] _freeSprites = Array.Empty<int>();
        private int _freeCount;
        private int _aliveCount;
        private float _emitAccumulator;
        private bool _initialized;
        private uint _rngState = 0x9E3779B9u;

        /// <summary>
        /// Current number of live particles.
        /// </summary>
        public int AliveCount => _aliveCount;

        public void SetBaseSorting(int baseSorting, bool applyToAlive = true)
        {
            if (BaseSorting == baseSorting && !applyToAlive) return;
            BaseSorting = baseSorting;

            if (!applyToAlive || !_initialized) return;

            if (_aliveCount == 0) return;

            for (var i = 0; i < _aliveCount; i++)
            {
                ref var particle = ref _particles[i];
                var sprite = _sprites[particle.SpriteIndex];
                var visualOffset = 0;
                if (particle.VisualIndex >= 0 && particle.VisualIndex < Visuals.Length)
                {
                    visualOffset = Visuals[particle.VisualIndex].SortingOffset;
                }
                sprite.Sorting = BaseSorting + visualOffset;
            }
        }

        protected override void Awake()
        {
            if (TargetLayer == null)
            {
                TargetLayer = Scene?.MainLayer;
            }

            InitializePool();
        }

        protected override void OnEnable()
        {
            if (!_initialized) return;
            for (var i = 0; i < _aliveCount; i++)
            {
                var sprite = _sprites[_particles[i].SpriteIndex];
                sprite.Visible = true;
            }
        }

        protected override void OnDisable()
        {
            if (!_initialized) return;
            for (var i = 0; i < _sprites.Length; i++)
            {
                _sprites[i].Visible = false;
            }
        }

        protected override void Update(float dt)
        {
            if (!_initialized) return;

            if (Emitting && RatePerSecond > 0f)
            {
                _emitAccumulator += RatePerSecond * dt;
                var emitCount = (int) _emitAccumulator;
                if (emitCount > 0)
                {
                    _emitAccumulator -= emitCount;
                    Emit(emitCount);
                }
            }

            if (_aliveCount == 0) return;

            var dragFactor = Drag > 0f ? MathF.Max(0f, 1f - Drag * dt) : 1f;

            for (var i = 0; i < _aliveCount;)
            {
                ref var particle = ref _particles[i];
                particle.Age += dt;
                if (particle.Age >= particle.Lifetime)
                {
                    KillParticle(i);
                    continue;
                }

                var t = particle.Lifetime > 0f ? particle.Age / particle.Lifetime : 1f;
                particle.Velocity += Gravity * dt;
                if (dragFactor < 1f)
                {
                    particle.Velocity *= dragFactor;
                }
                particle.Position += particle.Velocity * dt;
                particle.Rotation = (float) MathHelper.NormalizeNor(particle.Rotation + particle.AngularVelocity * dt);

                var sprite = _sprites[particle.SpriteIndex];
                var visual = Visuals.Length > 0 ? Visuals[particle.VisualIndex] : null;

                sprite.Position = particle.Position;
                sprite.Color = Multiply(ColorOverLife.Evaluate(t), particle.ColorScale);

                var sizeScale = SizeOverLife.Evaluate(t);
                var baseWidth = visual?.Width ?? sprite.Width;
                var baseHeight = visual?.Height ?? sprite.Height;

                sprite.Width = (int) MathF.Max(1f, MathF.Round(baseWidth * particle.Size * sizeScale));
                sprite.Height = (int) MathF.Max(1f, MathF.Round(baseHeight * particle.Size * sizeScale));
                sprite.Transformation.Rotation.Angle = particle.Rotation;
                sprite.Transformation.Rotation.Clockwise = RotationClockwise;

                i++;
            }
        }

        protected override void OnDestroy()
        {
            if (!_initialized) return;

            if (TargetLayer != null)
            {
                for (var i = 0; i < _sprites.Length; i++)
                {
                    TargetLayer.Remove(_sprites[i]);
                }
            }

            _sprites = Array.Empty<Sprite>();
            _particles = Array.Empty<Particle>();
            _freeSprites = Array.Empty<int>();
            _aliveCount = 0;
            _freeCount = 0;
            _initialized = false;
        }

        /// <summary>
        /// Emits a fixed number of particles immediately, respecting pool capacity.
        /// This is useful for bursts (explosions, impacts) and works even when
        /// <see cref="Emitting"/> is false.
        /// </summary>
        /// <param name="count">Desired particle count for this burst.</param>
        public void Emit(int count)
        {
            if (!_initialized) return;
            if (Visuals.Length == 0 || count <= 0) return;

            for (var i = 0; i < count; i++)
            {
                if (_freeCount == 0) break;

                var spriteIndex = _freeSprites[--_freeCount];
                var visualIndex = NextInt(0, Visuals.Length - 1);
                var visual = Visuals[visualIndex];

                var lifetime = RandomRange(LifetimeMin, LifetimeMax);
                if (lifetime <= 0f)
                {
                    _freeSprites[_freeCount++] = spriteIndex;
                    continue;
                }

                var spawnPos = ResolveEmitterPosition() + RandomSpawnOffset();
                var velocity = RandomRange(VelocityMin, VelocityMax);
                var size = RandomRange(SizeMin, SizeMax);

                var rotation = (float) MathHelper.NormalizeNor(RandomRange(RotationMin, RotationMax));
                var angularVelocity = RandomRange(AngularVelocityMin, AngularVelocityMax);

                var colorScale = RandomRange(ColorMin, ColorMax);

                ApplyVisual(spriteIndex, visual);

                _particles[_aliveCount++] = new Particle
                {
                    Position = spawnPos,
                    Velocity = velocity,
                    Rotation = rotation,
                    AngularVelocity = angularVelocity,
                    Age = 0f,
                    Lifetime = lifetime,
                    Size = size,
                    ColorScale = colorScale,
                    VisualIndex = visualIndex,
                    SpriteIndex = spriteIndex
                };
            }
        }

        /// <summary>
        /// Kills all live particles and resets emission accumulator.
        /// Use this when reusing a system for a new effect.
        /// </summary>
        public void Clear()
        {
            if (!_initialized) return;

            for (var i = 0; i < _sprites.Length; i++)
            {
                _sprites[i].Visible = false;
                _freeSprites[i] = i;
            }

            _freeCount = _sprites.Length;
            _aliveCount = 0;
            _emitAccumulator = 0f;
        }

        private void InitializePool()
        {
            if (_initialized) return;
            if (TargetLayer == null) return;

            var capacity = Capacity < 0 ? 0 : Capacity;
            _particles = new Particle[capacity];
            _sprites = new Sprite[capacity];
            _freeSprites = new int[capacity];

            for (var i = 0; i < capacity; i++)
            {
                var sprite = new Sprite(1, 1)
                {
                    Visible = false,
                    OriginPoint = OriginPoint.Centered,
                    Sorting = BaseSorting
                };
                _sprites[i] = sprite;
                _freeSprites[i] = i;
                TargetLayer.Add(sprite);
            }

            _freeCount = capacity;
            _aliveCount = 0;
            _emitAccumulator = 0f;
            _initialized = true;

            ColorOverLife.Prepare();
            SizeOverLife.Prepare();
        }

        private void ApplyVisual(int spriteIndex, ParticleVisual visual)
        {
            var sprite = _sprites[spriteIndex];
            sprite.Texture = visual.Texture;
            sprite.Material = visual.Material;
            sprite.OriginPoint = visual.OriginPoint;
            sprite.Sorting = BaseSorting + visual.SortingOffset;
            sprite.Width = visual.Width;
            sprite.Height = visual.Height;
            sprite.Visible = true;
        }

        private void KillParticle(int index)
        {
            var spriteIndex = _particles[index].SpriteIndex;
            _sprites[spriteIndex].Visible = false;
            _freeSprites[_freeCount++] = spriteIndex;

            _aliveCount--;
            if (index < _aliveCount)
            {
                _particles[index] = _particles[_aliveCount];
            }
        }

        private Vector2 ResolveEmitterPosition()
        {
            var basePosition = Position;
            if (UseEntityTransform && Entity != null)
            {
                basePosition = Entity.Transform.CanvasPosition;
            }

            return basePosition + Offset;
        }

        private Vector2 RandomSpawnOffset()
        {
            switch (SpawnShape)
            {
                case ParticleSpawnShape.Circle:
                {
                    var angle = NextFloat() * MathF.PI * 2f;
                    var radius = MathF.Sqrt(NextFloat()) * SpawnRadius;
                    return new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
                }
                case ParticleSpawnShape.Box:
                {
                    var half = SpawnBoxSize * 0.5f;
                    return new Vector2(
                        RandomRange(-half.X, half.X),
                        RandomRange(-half.Y, half.Y)
                    );
                }
                default:
                    return Vector2.Zero;
            }
        }

        private float NextFloat()
        {
            _rngState ^= _rngState << 13;
            _rngState ^= _rngState >> 17;
            _rngState ^= _rngState << 5;
            return (_rngState & 0x00FFFFFF) / (float) 0x01000000;
        }

        private int NextInt(int min, int max)
        {
            if (min >= max) return min;
            var range = max - min + 1;
            return min + (int) (NextFloat() * range);
        }

        private float RandomRange(float min, float max)
        {
            if (min >= max) return min;
            return min + (max - min) * NextFloat();
        }

        private Vector2 RandomRange(Vector2 min, Vector2 max)
        {
            return new Vector2(
                RandomRange(min.X, max.X),
                RandomRange(min.Y, max.Y)
            );
        }

        private Vector4 RandomRange(Vector4 min, Vector4 max)
        {
            return new Vector4(
                RandomRange(min.X, max.X),
                RandomRange(min.Y, max.Y),
                RandomRange(min.Z, max.Z),
                RandomRange(min.W, max.W)
            );
        }

        private static Vector4 Multiply(Vector4 a, Vector4 b)
        {
            return new Vector4(a.X * b.X, a.Y * b.Y, a.Z * b.Z, a.W * b.W);
        }
    }
}
