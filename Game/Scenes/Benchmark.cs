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
            CompositeOutline
        }

        private readonly List<Sprite> _sprites = new();
        private readonly List<Light2D> _lights = new();

        private readonly BenchmarkBuildState _build = new();

        private const int DefaultSpriteCount = 50_000;
        private const int MinSpriteCount = 1_000;
        private const int MaxSpriteCount = 100_000;
        private const int BuildBatchSize = 1_500;

        private int _targetSpriteCount = DefaultSpriteCount;
        private BenchmarkProfile _profile = BenchmarkProfile.SpriteThroughput;
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
            new("./resources/data/textures/vfx/particles/circle_02.png", 512, 512)
        };

        private readonly TextureSpec[] _litTextures =
        {
            new("./resources/data/textures/stone0.png", 256, 256),
            new("./resources/data/textures/vfx/particles/smoke_01.png", 512, 512),
            new("./resources/data/textures/vfx/particles/star_01.png", 512, 512)
        };

        private readonly TextureSpec[] _compositeTextures =
        {
            new("./resources/data/textures/vfx/particles/flame_01.png", 512, 512),
            new("./resources/data/textures/vfx/particles/spark_01.png", 512, 512),
            new("./resources/data/textures/vfx/particles/smoke_03.png", 512, 512)
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
            Console.WriteLine("Keys: 1 throughput, 2 lit/emission, 3 composite/outline, +/- count, R rebuild, C camera pan");

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
            _build.Rows = (spriteCount + _build.GridWidth - 1) / _build.GridWidth;

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
        }

        private Sprite CreateSprite(int index, BenchmarkBuildState build)
        {
            var gridX = index % build.GridWidth;
            var gridY = index / build.GridWidth;
            var jitter = Hash01(index);
            var jitter2 = Hash01(index * 17 + 31);

            var x = gridX * build.Spacing + (jitter - 0.5f) * 4f;
            var y = gridY * build.Spacing + (jitter2 - 0.5f) * 4f;

            var textureSpec = SelectTextureSpec(build.Profile, index);
            var texture = Texture.AcquireShared(textureSpec.Path, textureSpec.Width, textureSpec.Height);

            var sprite = new Sprite
            {
                Name = $"bench_{index}",
                Texture = texture,
                Width = 32,
                Height = 32,
                Position = new Vector2(x, y),
                OriginPoint = OriginPoint.Centered,
                Sorting = gridY * 10 + (gridX % 10),
                Color = SelectColor(build.Profile, index),
                BlendMode = SelectBlendMode(build.Profile, index),
                Material = SelectMaterial(build.Profile, index)
            };

            if (build.Profile == BenchmarkProfile.CompositeOutline && (index % 3 == 0))
            {
                sprite.Outline.Enabled = true;
                sprite.Outline.ThicknessTexels = 1.2f + Hash01(index * 7 + 11) * 1.2f;
                sprite.Outline.Color = new Vector4(0.95f, 0.95f, 1f, 0.85f);
                sprite.Outline.Layering = (index & 1) == 0 ? OutlineLayering.Under : OutlineLayering.Over;
            }

            return sprite;
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
                {
                    Lighting.AmbientColor = new Vector3(0.9f, 0.95f, 1f);
                    Lighting.AmbientIntensity = 0.22f;

                    _emissionMap = Texture.AcquireShared("./resources/data/textures/stone0_em.png", 256, 256);
                    _emissiveMaterial = SpriteMaterialFactory.LitEmissiveAutoNormal(
                        new Vector3(0.35f, 0.9f, 0.55f),
                        2.25f,
                        _emissionMap
                    );

                    AddBenchmarkLight(new Vector2(_worldWidth * 0.3f, _worldHeight * 0.3f), new Vector3(1f, 0.92f, 0.8f), 2.2f, 620f);
                    AddBenchmarkLight(new Vector2(_worldWidth * 0.7f, _worldHeight * 0.3f), new Vector3(0.75f, 0.9f, 1f), 2.0f, 640f);
                    AddBenchmarkLight(new Vector2(_worldWidth * 0.3f, _worldHeight * 0.72f), new Vector3(0.7f, 1f, 0.75f), 1.8f, 560f);
                    AddBenchmarkLight(new Vector2(_worldWidth * 0.72f, _worldHeight * 0.72f), new Vector3(1f, 0.7f, 0.75f), 1.8f, 560f);

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
                    break;
                }

                case BenchmarkProfile.CompositeOutline:
                    Lighting.AmbientIntensity = 0f;
                    break;
            }
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

        private TextureSpec SelectTextureSpec(BenchmarkProfile profile, int index)
        {
            return profile switch
            {
                BenchmarkProfile.SpriteThroughput => _throughputTextures[index % _throughputTextures.Length],
                BenchmarkProfile.LitNormalEmission => _litTextures[index % _litTextures.Length],
                BenchmarkProfile.CompositeOutline => _compositeTextures[index % _compositeTextures.Length],
                _ => _throughputTextures[0]
            };
        }

        private IMaterial SelectMaterial(BenchmarkProfile profile, int index)
        {
            return profile switch
            {
                BenchmarkProfile.SpriteThroughput => _unlitMaterial,
                BenchmarkProfile.LitNormalEmission => index % 7 == 0 && _emissiveMaterial != null
                    ? _emissiveMaterial
                    : _litMaterial,
                BenchmarkProfile.CompositeOutline => _unlitMaterial,
                _ => _unlitMaterial
            };
        }

        private static Vector4 SelectColor(BenchmarkProfile profile, int index)
        {
            var a = 0.8f + Hash01(index * 5 + 7) * 0.2f;

            return profile switch
            {
                BenchmarkProfile.SpriteThroughput => new Vector4(0.95f, 0.95f, 0.95f, a),
                BenchmarkProfile.LitNormalEmission => new Vector4(0.85f + Hash01(index) * 0.15f, 0.9f, 0.95f, a),
                BenchmarkProfile.CompositeOutline => new Vector4(0.9f, 0.85f + Hash01(index * 3) * 0.15f, 0.8f, a),
                _ => Vector4.One
            };
        }

        private static SpriteBlendMode SelectBlendMode(BenchmarkProfile profile, int index)
        {
            if (profile != BenchmarkProfile.CompositeOutline)
            {
                return SpriteBlendMode.Normal;
            }

            return CompositeBlendModes[index % CompositeBlendModes.Length];
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

        private sealed class BenchmarkBuildState
        {
            public bool IsBuilding;
            public BenchmarkProfile Profile;
            public int TargetSpriteCount;
            public int NextIndex;
            public int GridWidth;
            public int Rows;
            public float Spacing;
        }
    }
}
