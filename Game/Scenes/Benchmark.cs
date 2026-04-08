using System.Collections;
using System.Numerics;

namespace IsometricMagic.Game.Scenes
{
    public sealed class Benchmark : Scene
    {
        private enum BenchmarkProfile
        {
            SpriteThroughput,
            LitNormalEmission,
            CompositeOutline,
            MainMixed,
        }

        private enum SpriteKind
        {
            Throughput,
            Lit,
            Composite,
        }

        private readonly List<Sprite> _sprites = new();
        private readonly List<Light2D> _lights = new();

        private readonly BenchmarkBuildState _build = new();

        private const int DefaultSpriteCount = 25_000;
        private const int MinSpriteCount = 1_000;
        private const int MaxSpriteCount = 25_000;
        private const int BuildBatchSize = 1_500;

        private int _targetSpriteCount = DefaultSpriteCount;
        private BenchmarkProfile _profile = BenchmarkProfile.MainMixed;
        private bool _cameraAutoPan = true;
        private float _cameraPanTime;

        private float _worldWidth;
        private float _worldHeight;

        private readonly IMaterial _unlitMaterial = SpriteMaterialFactory.Unlit();
        private readonly IMaterial _litMaterial = SpriteMaterialFactory.LitAutoNormal();
        private IMaterial? _emissiveMaterial;
        private Texture? _emissionMap;

        private readonly TextureSpec[] _throughputTextures =
        {
            new("./resources/data/textures/circle_128.png", 128, 128),
            new("./resources/data/textures/loading_circle.png", 128, 128),
            new("./resources/data/textures/loading_text.png", 288, 49),
            new("./resources/data/textures/vfx/particles/circle_02.png", 512, 512),
            new("./resources/data/textures/ts1/stone_E.png", 256, 512)
        };

        private readonly TextureSpec[] _litTextures =
        {
            new("./resources/data/textures/stone0.png", 256, 256),
            new("./resources/data/textures/characters/man/animations/running/135/Frame0.png", 256, 256),
            new("./resources/data/textures/characters/man/animations/dying/315/Frame4.png", 256, 256),
            new("./resources/data/textures/vfx/particles/smoke_01.png", 512, 512),
            new("./resources/data/textures/vfx/particles/star_01.png", 512, 512)
        };

        private readonly TextureSpec[] _compositeTextures =
        {
            new("./resources/data/textures/vfx/particles/flame_01.png", 512, 512),
            new("./resources/data/textures/vfx/particles/spark_01.png", 512, 512),
            new("./resources/data/textures/vfx/particles/smoke_03.png", 512, 512),
            new("./resources/data/textures/vfx/particles/window_02.png", 512, 512),
            new("./resources/data/textures/vfx/particles/trace_07.png", 512, 512)
        };

        private static readonly SizeSpec[] ThroughputSizes =
        {
            new(16, 16),
            new(24, 24),
            new(32, 32),
            new(40, 40),
            new(48, 48),
            new(64, 32),
            new(32, 64),
            new(32, 64),
            new(256, 512),
            new(512, 512),
        };

        private static readonly SizeSpec[] LitSizes =
        {
            new(24, 24),
            new(32, 32),
            new(48, 48),
            new(64, 64),
            new(96, 64),
            new(64, 96),
            new(128, 128),
            new(512, 512),
        };

        private static readonly SizeSpec[] CompositeSizes =
        {
            new(128, 128),
            new(160, 160),
            new(192, 96),
            new(96, 192),
            new(256, 256),
            new(512, 512),
            new(512, 256),
        };

        private static readonly OriginPoint[] ThroughputOrigins =
        {
            OriginPoint.Centered,
            OriginPoint.LeftTop,
            OriginPoint.BottomCenter,
            OriginPoint.RightCenter,
        };

        private static readonly OriginPoint[] LitOrigins =
        {
            OriginPoint.Centered,
            OriginPoint.BottomCenter,
            OriginPoint.TopCenter,
            OriginPoint.LeftCenter,
            OriginPoint.RightCenter,
        };

        private static readonly OriginPoint[] CompositeOrigins =
        {
            OriginPoint.Centered,
            OriginPoint.LeftTop,
            OriginPoint.RightTop,
            OriginPoint.LeftBottom,
            OriginPoint.RightBottom,
            OriginPoint.BottomCenter,
        };

        private static readonly SpriteBlendMode[] CompositeBlendModes =
        {
            SpriteBlendMode.Multiply,
            SpriteBlendMode.Screen,
            SpriteBlendMode.SoftLight,
            SpriteBlendMode.Overlay
        };

        public Benchmark() : base("benchmark", true)
        {
        }

        protected override IEnumerator InitializeAsync()
        {
            Console.WriteLine("Benchmark scene initialized");
            Console.WriteLine("Keys: 1 throughput, 2 lit/emission, 3 composite/outline, 4 main(80/19/1), +/- count, R rebuild, C camera pan");

            QueueBuild(_profile, _targetSpriteCount);
            while (_build.IsBuilding)
            {
                BuildStep();
                yield return true;
            }
        }

        public override void Update()
        {
            HandleInput();

            if (_build.IsBuilding)
            {
                BuildStep();
            }

            if (_cameraAutoPan)
            {
                UpdateCameraPan(Time.DeltaTime);
            }
        }

        protected override void DeInitialize()
        {
            ClearCurrentContent();
        }

        private void HandleInput()
        {
            if (IsPressed(Key.Num1, Key.Keypad1))
            {
                QueueBuild(BenchmarkProfile.SpriteThroughput, _targetSpriteCount);
            }
            else if (IsPressed(Key.Num2, Key.Keypad2))
            {
                QueueBuild(BenchmarkProfile.LitNormalEmission, _targetSpriteCount);
            }
            else if (IsPressed(Key.Num3, Key.Keypad3))
            {
                QueueBuild(BenchmarkProfile.CompositeOutline, _targetSpriteCount);
            }
            else if (IsPressed(Key.Num4, Key.Keypad4))
            {
                QueueBuild(BenchmarkProfile.MainMixed, _targetSpriteCount);
            }

            if (Input.WasPressed(Key.Equals) || Input.WasPressed(Key.KeypadPlus))
            {
                _targetSpriteCount = Math.Min(MaxSpriteCount, _targetSpriteCount + 5_000);
                QueueBuild(_profile, _targetSpriteCount);
            }
            else if (Input.WasPressed(Key.Minus) || Input.WasPressed(Key.KeypadMinus))
            {
                _targetSpriteCount = Math.Max(MinSpriteCount, _targetSpriteCount - 5_000);
                QueueBuild(_profile, _targetSpriteCount);
            }

            if (Input.WasPressed(Key.R))
            {
                QueueBuild(_profile, _targetSpriteCount);
            }

            if (Input.WasPressed(Key.C))
            {
                _cameraAutoPan = !_cameraAutoPan;
            }
        }

        private static bool IsPressed(Key a, Key b)
        {
            return Input.WasPressed(a) || Input.WasPressed(b);
        }

        private void QueueBuild(BenchmarkProfile profile, int spriteCount)
        {
            _profile = profile;
            _targetSpriteCount = spriteCount;

            Console.WriteLine($"Benchmark: rebuilding profile={profile} sprites={spriteCount}");

            ClearCurrentContent();

            _build.IsBuilding = true;
            _build.Profile = profile;
            _build.TargetSpriteCount = spriteCount;
            _build.NextIndex = 0;
            _build.GridWidth = (int)MathF.Ceiling(MathF.Sqrt(spriteCount));
            _build.Spacing = profile == BenchmarkProfile.CompositeOutline ? 30f : 24f;
            if (profile == BenchmarkProfile.MainMixed)
            {
                _build.Spacing = 26f;
            }

            _build.Rows = (spriteCount + _build.GridWidth - 1) / _build.GridWidth;
            _build.ThroughputTarget = spriteCount * 70 / 100;
            _build.LitTarget = spriteCount * 20 / 100;
            _build.CompositeTarget = spriteCount - _build.ThroughputTarget - _build.LitTarget;
            _build.ThroughputCount = 0;
            _build.LitCount = 0;
            _build.CompositeCount = 0;
            _build.RotatedCount = 0;
            _build.OutlineCount = 0;

            _worldWidth = _build.GridWidth * _build.Spacing;
            _worldHeight = _build.Rows * _build.Spacing;

            ConfigureProfile(profile);
            CenterCamera();
        }

        private void BuildStep()
        {
            if (!_build.IsBuilding)
            {
                return;
            }

            var end = Math.Min(_build.NextIndex + BuildBatchSize, _build.TargetSpriteCount);
            for (var i = _build.NextIndex; i < end; i++)
            {
                var sprite = CreateSprite(i, _build);
                _sprites.Add(sprite);
                MainLayer.Add(sprite);
            }

            _build.NextIndex = end;
            if (_build.NextIndex < _build.TargetSpriteCount)
            {
                return;
            }

            _build.IsBuilding = false;
            Console.WriteLine($"Benchmark: ready profile={_build.Profile} sprites={_build.TargetSpriteCount}");
            PrintBuildSummary(_build);
        }

        private Sprite CreateSprite(int index, BenchmarkBuildState build)
        {
            var kind = SelectSpriteKind(build, index);
            var gridX = index % build.GridWidth;
            var gridY = index / build.GridWidth;
            var jitter = Hash01(index);
            var jitter2 = Hash01(index * 17 + 31);

            var x = gridX * build.Spacing + (jitter - 0.5f) * 4f;
            var y = gridY * build.Spacing + (jitter2 - 0.5f) * 4f;

            var textureSpec = SelectTextureSpec(kind, index);
            var size = SelectSize(kind, index);
            var texture = Texture.AcquireShared(textureSpec.Path, textureSpec.Width, textureSpec.Height);
            var rotated = ShouldRotate(kind, index);

            var sprite = new Sprite
            {
                Name = $"bench_{index}",
                Texture = texture,
                Width = size.Width,
                Height = size.Height,
                Position = new Vector2(x, y),
                OriginPoint = SelectOrigin(kind, index),
                Sorting = gridY * 10 + (gridX % 10),
                Color = SelectColor(kind, index),
                BlendMode = SelectBlendMode(kind, index),
                Material = SelectMaterial(kind, index)
            };

            if (rotated)
            {
                sprite.Transformation.Rotation.Angle = 0.02 + Hash01(index * 43 + 19) * 0.96;
                sprite.Transformation.Rotation.Clockwise = Hash01(index * 59 + 23) >= 0.5f;
            }
            else
            {
                sprite.Transformation.Rotation.Angle = 0d;
            }

            var enableOutline = ShouldEnableOutline(build.Profile, kind, index);
            if (enableOutline)
            {
                sprite.Outline.Enabled = true;
                sprite.Outline.ThicknessTexels = 1.2f + Hash01(index * 7 + 11) * 1.2f;
                sprite.Outline.Color = new Vector4(0.95f, 0.95f, 1f, 0.85f);
                sprite.Outline.Layering = (index & 1) == 0 ? OutlineLayering.Under : OutlineLayering.Over;
            }

            RecordBuildStats(build, kind, rotated, enableOutline);
            return sprite;
        }

        private static SpriteKind SelectSpriteKind(BenchmarkBuildState build, int index)
        {
            return build.Profile switch
            {
                BenchmarkProfile.SpriteThroughput => SpriteKind.Throughput,
                BenchmarkProfile.LitNormalEmission => SpriteKind.Lit,
                BenchmarkProfile.CompositeOutline => SpriteKind.Composite,
                BenchmarkProfile.MainMixed => SelectMixedSpriteKind(build, index),
                _ => SpriteKind.Throughput
            };
        }

        private static SpriteKind SelectMixedSpriteKind(BenchmarkBuildState build, int index)
        {
            var value = Hash01(index * 97 + 131);
            var preferred = value switch
            {
                < 0.80f => SpriteKind.Throughput,
                < 0.99f => SpriteKind.Lit,
                _ => SpriteKind.Composite,
            };

            if (HasRemainingQuota(build, preferred))
            {
                return preferred;
            }

            if (HasRemainingQuota(build, SpriteKind.Throughput))
            {
                return SpriteKind.Throughput;
            }

            if (HasRemainingQuota(build, SpriteKind.Lit))
            {
                return SpriteKind.Lit;
            }

            return SpriteKind.Composite;
        }

        private static bool HasRemainingQuota(BenchmarkBuildState build, SpriteKind kind)
        {
            return kind switch
            {
                SpriteKind.Throughput => build.ThroughputCount < build.ThroughputTarget,
                SpriteKind.Lit => build.LitCount < build.LitTarget,
                SpriteKind.Composite => build.CompositeCount < build.CompositeTarget,
                _ => false,
            };
        }

        private void ConfigureProfile(BenchmarkProfile profile)
        {
            PostProcess.Clear();
            Lighting.Clear();
            _lights.Clear();

            if (_emissionMap != null)
            {
                _emissionMap.Destroy();
                _emissionMap = null;
                _emissiveMaterial = null;
            }

            switch (profile)
            {
                case BenchmarkProfile.SpriteThroughput:
                    Lighting.AmbientIntensity = 0f;
                    break;

                case BenchmarkProfile.LitNormalEmission:
                case BenchmarkProfile.MainMixed:
                {
                    ConfigureLitPipeline();
                    break;
                }

                case BenchmarkProfile.CompositeOutline:
                    Lighting.AmbientIntensity = 0f;
                    break;
            }
        }

        private void ConfigureLitPipeline()
        {
            Lighting.AmbientColor = new Vector3(0.9f, 0.95f, 1f);
            Lighting.AmbientIntensity = 0.22f;

            _emissionMap = Texture.AcquireShared("./resources/data/textures/stone0_em.png", 256, 256);
            _emissiveMaterial = SpriteMaterialFactory.LitEmissiveAutoNormal(
                new Vector3(0.35f, 0.9f, 0.55f),
                2.25f,
                _emissionMap
            );

            AddBenchmarkLight(new Vector2(_worldWidth * 0.3f, _worldHeight * 0.3f), new Vector3(1f, 0.92f, 0.8f), 2.2f,
                620f);
            AddBenchmarkLight(new Vector2(_worldWidth * 0.7f, _worldHeight * 0.3f), new Vector3(0.75f, 0.9f, 1f), 2.0f,
                640f);
            AddBenchmarkLight(new Vector2(_worldWidth * 0.3f, _worldHeight * 0.72f), new Vector3(0.7f, 1f, 0.75f), 1.8f,
                560f);
            AddBenchmarkLight(new Vector2(_worldWidth * 0.72f, _worldHeight * 0.72f), new Vector3(1f, 0.7f, 0.75f), 1.8f,
                560f);

            PostProcess.Add(new BloomEffect
            {
                Threshold = 1.1f,
                Knee = 0.6f,
                Intensity = 0.85f,
                BlurIterations = 3
            });
            PostProcess.Add(new ToneMapEffect
            {
                Exposure = 0.24f
            });
        }

        private void AddBenchmarkLight(Vector2 position, Vector3 color, float intensity, float radius)
        {
            var light = new Light2D(position)
            {
                Color = color,
                Intensity = intensity,
                Radius = radius,
                Height = 1.8f,
                Falloff = 2.2f,
                InnerRadius = 64f,
                CenterAttenuation = 0.25f
            };

            _lights.Add(light);
            Lighting.Add(light);
        }

        private TextureSpec SelectTextureSpec(SpriteKind kind, int index)
        {
            return kind switch
            {
                SpriteKind.Throughput => _throughputTextures[index % _throughputTextures.Length],
                SpriteKind.Lit => _litTextures[index % _litTextures.Length],
                SpriteKind.Composite => _compositeTextures[index % _compositeTextures.Length],
                _ => _throughputTextures[0]
            };
        }

        private IMaterial SelectMaterial(SpriteKind kind, int index)
        {
            return kind switch
            {
                SpriteKind.Throughput => _unlitMaterial,
                SpriteKind.Lit => index % 7 == 0 && _emissiveMaterial != null
                    ? _emissiveMaterial
                    : _litMaterial,
                SpriteKind.Composite => _unlitMaterial,
                _ => _unlitMaterial
            };
        }

        private static Vector4 SelectColor(SpriteKind kind, int index)
        {
            var a = 0.8f + Hash01(index * 5 + 7) * 0.2f;

            return kind switch
            {
                SpriteKind.Throughput => new Vector4(0.95f, 0.95f, 0.95f, a),
                SpriteKind.Lit => new Vector4(0.85f + Hash01(index) * 0.15f, 0.9f, 0.95f, a),
                SpriteKind.Composite => new Vector4(0.9f, 0.85f + Hash01(index * 3) * 0.15f, 0.8f, a),
                _ => Vector4.One
            };
        }

        private static SpriteBlendMode SelectBlendMode(SpriteKind kind, int index)
        {
            if (kind != SpriteKind.Composite)
            {
                return SpriteBlendMode.Normal;
            }

            return CompositeBlendModes[index % CompositeBlendModes.Length];
        }

        private static SizeSpec SelectSize(SpriteKind kind, int index)
        {
            return kind switch
            {
                SpriteKind.Throughput => ThroughputSizes[index % ThroughputSizes.Length],
                SpriteKind.Lit => LitSizes[index % LitSizes.Length],
                SpriteKind.Composite => CompositeSizes[index % CompositeSizes.Length],
                _ => new SizeSpec(32, 32),
            };
        }

        private static OriginPoint SelectOrigin(SpriteKind kind, int index)
        {
            return kind switch
            {
                SpriteKind.Throughput => ThroughputOrigins[index % ThroughputOrigins.Length],
                SpriteKind.Lit => LitOrigins[index % LitOrigins.Length],
                SpriteKind.Composite => CompositeOrigins[index % CompositeOrigins.Length],
                _ => OriginPoint.Centered,
            };
        }

        private static bool ShouldRotate(SpriteKind kind, int index)
        {
            var chance = kind switch
            {
                SpriteKind.Throughput => 0.15f,
                SpriteKind.Lit => 0.25f,
                SpriteKind.Composite => 0.35f,
                _ => 0f,
            };

            return Hash01(index * 29 + 47) < chance;
        }

        private static bool ShouldEnableOutline(BenchmarkProfile profile, SpriteKind kind, int index)
        {
            if (profile == BenchmarkProfile.CompositeOutline)
            {
                return index % 3 == 0;
            }

            if (profile == BenchmarkProfile.MainMixed && kind == SpriteKind.Composite)
            {
                return index % 3 == 0;
            }

            return false;
        }

        private static void RecordBuildStats(BenchmarkBuildState build, SpriteKind kind, bool rotated, bool outlined)
        {
            switch (kind)
            {
                case SpriteKind.Throughput:
                    build.ThroughputCount++;
                    break;

                case SpriteKind.Lit:
                    build.LitCount++;
                    break;

                case SpriteKind.Composite:
                    build.CompositeCount++;
                    break;
            }

            if (rotated)
            {
                build.RotatedCount++;
            }

            if (outlined)
            {
                build.OutlineCount++;
            }
        }

        private static void PrintBuildSummary(BenchmarkBuildState build)
        {
            if (build.TargetSpriteCount <= 0)
            {
                return;
            }

            var total = (float) build.TargetSpriteCount;
            var throughputPct = build.ThroughputCount / total * 100f;
            var litPct = build.LitCount / total * 100f;
            var compositePct = build.CompositeCount / total * 100f;
            var rotatedPct = build.RotatedCount / total * 100f;
            var outlinePct = build.OutlineCount / total * 100f;

            Console.WriteLine(
                $"Benchmark mix: throughput={build.ThroughputCount} ({throughputPct:F1}%), lit={build.LitCount} ({litPct:F1}%), composite={build.CompositeCount} ({compositePct:F1}%), rotated={build.RotatedCount} ({rotatedPct:F1}%), outlined={build.OutlineCount} ({outlinePct:F1}%)");
        }

        private void CenterCamera()
        {
            var maxX = Math.Max(0f, _worldWidth - Camera.Rect.Width);
            var maxY = Math.Max(0f, _worldHeight - Camera.Rect.Height);

            Camera.Rect.X = (int)Math.Clamp((_worldWidth - Camera.Rect.Width) * 0.5f, 0f, maxX);
            Camera.Rect.Y = (int)Math.Clamp((_worldHeight - Camera.Rect.Height) * 0.5f, 0f, maxY);
        }

        private void UpdateCameraPan(float dt)
        {
            if (_worldWidth <= Camera.Rect.Width || _worldHeight <= Camera.Rect.Height)
            {
                return;
            }

            _cameraPanTime += dt * 0.25f;

            var centerX = _worldWidth * 0.5f + MathF.Cos(_cameraPanTime) * _worldWidth * 0.35f;
            var centerY = _worldHeight * 0.5f + MathF.Sin(_cameraPanTime * 0.73f) * _worldHeight * 0.35f;

            var maxX = Math.Max(0f, _worldWidth - Camera.Rect.Width);
            var maxY = Math.Max(0f, _worldHeight - Camera.Rect.Height);
            var nextX = Math.Clamp(centerX - Camera.Rect.Width * 0.5f, 0f, maxX);
            var nextY = Math.Clamp(centerY - Camera.Rect.Height * 0.5f, 0f, maxY);

            Camera.Rect.X = (int)nextX;
            Camera.Rect.Y = (int)nextY;
        }

        private void ClearCurrentContent()
        {
            foreach (var sprite in _sprites)
            {
                MainLayer.Remove(sprite);
                sprite.Texture?.Destroy();
            }

            _sprites.Clear();

            foreach (var light in _lights)
            {
                Lighting.Remove(light);
            }

            _lights.Clear();
            Lighting.Clear();
            PostProcess.Clear();

            if (_emissionMap != null)
            {
                _emissionMap.Destroy();
                _emissionMap = null;
                _emissiveMaterial = null;
            }

            _build.IsBuilding = false;
            _build.NextIndex = 0;
        }

        private static float Hash01(int value)
        {
            var x = (uint)value;
            x ^= x >> 16;
            x *= 0x7feb352dU;
            x ^= x >> 15;
            x *= 0x846ca68bU;
            x ^= x >> 16;
            return (x & 0x00ffffffU) / 16777215f;
        }

        private readonly record struct TextureSpec(string Path, int Width, int Height);
        private readonly record struct SizeSpec(int Width, int Height);

        private sealed class BenchmarkBuildState
        {
            public bool IsBuilding;
            public BenchmarkProfile Profile;
            public int TargetSpriteCount;
            public int NextIndex;
            public int GridWidth;
            public int Rows;
            public float Spacing;
            public int ThroughputTarget;
            public int LitTarget;
            public int CompositeTarget;
            public int ThroughputCount;
            public int LitCount;
            public int CompositeCount;
            public int RotatedCount;
            public int OutlineCount;
        }
    }
}
