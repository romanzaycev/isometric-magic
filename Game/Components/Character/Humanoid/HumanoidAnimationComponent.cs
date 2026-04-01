using System.Collections.Generic;
using System.Numerics;
using IsometricMagic.Engine;
using IsometricMagic.Engine.Graphics.Materials;
using IsometricMagic.Game.Animation;
using IsometricMagic.Game.Components.Actor;
using IsometricMagic.Game.Model;

namespace IsometricMagic.Game.Components.Character.Humanoid
{
    public class HumanoidAnimationComponent : Component
    {
        private MotorComponent? _motorComponent;
        
        private readonly Dictionary<string, Sequence> _animations = new();
        private Sequence? _currentSequence;
        private string _currentAnimationName = "idle_0";

        private WorldDirection _direction = WorldDirection.N;
        private LocomotionState _state = LocomotionState.Idle;

        private readonly List<Sprite> _managedSprites = new();

        public SceneLayer? TargetLayer { get; set; }
        public int Sorting { get; set; } = 1000;
        
        public WorldDirection Direction
        {
            get => _direction;
            set
            {
                if (_direction == value) return;
                _direction = value;
                UpdateAnimationState();
            }
        }

        public LocomotionState State
        {
            get => _state;
            set
            {
                if (_state == value) return;
                _state = value;
                UpdateAnimationState();
            }
        }

        protected override void Awake()
        {
            _motorComponent = Entity?.GetComponent<MotorComponent>();
            LoadAnimations();
            PlayAnimation("idle_0");
        }

        protected override void OnEnable()
        {
            if (_currentSequence != null)
            {
                _currentSequence.Play();
            }
        }

        protected override void OnDisable()
        {
            if (_currentSequence != null)
            {
                _currentSequence.Stop();
            }
        }

        protected override void Update(float dt)
        {
            if (_motorComponent != null)
            {
                _state = _motorComponent.State;
                _direction = _motorComponent.Direction;
                UpdateAnimationState();
            }
            
            if (_currentSequence == null) return;
            
            _currentSequence.Update(dt);
        }

        public Sequence? GetCurrentSequence() => _currentSequence;

        public Sprite? GetCurrentSprite() => _currentSequence?.CurrentSprite;

        private void LoadAnimations()
        {
            if (TargetLayer == null) return;

            string[] availableAnimations = { "idle", "dying", "running" };
            string[] availableDirections = { "0", "45", "90", "135", "180", "225", "270", "315" };

            const int totalFrames = 10;
            const string frameName = "Frame";
            const string extension = ".png";
            const string animationPath = "./resources/data/textures/characters/man/animations/{0}";

            foreach (var animationName in availableAnimations)
            {
                var path = string.Format(animationPath, animationName);

                foreach (var direction in availableDirections)
                {
                    var framesPath = $"{path}/{direction}";
                    var fullAnimationName = $"{animationName}_{direction}";
                    var sprites = new List<Sprite>();

                    for (var i = 0; i < totalFrames; i++)
                    {
                        var tex = new Texture(256, 256);
                        var sprite = new Sprite
                        {
                            Width = 256,
                            Height = 256,
                            Texture = tex,
                            OriginPoint = OriginPoint.BottomCenter,
                            Sorting = Sorting,
                            Transformation = { Translate = new Vector2(0, 8) },
                        };
                        tex.LoadImage($"{framesPath}/{frameName}{i}{extension}");
                        sprite.Material = new NormalMappedLitSpriteMaterial();
                        sprite.Outline.Enabled = true;
                        sprite.Outline.Color = new(0f, 0f, 0f, 0.5f);
                        sprite.Visible = false;

                        sprites.Add(sprite);
                        _managedSprites.Add(sprite);
                        TargetLayer.Add(sprite);
                    }

                    var seq = new Sequence(fullAnimationName, sprites) { FrameDelay = 0.08f };
                    _animations[fullAnimationName] = seq;
                }
            }
        }

        private void UpdateAnimationState()
        {
            var nextAnimation = _state switch
            {
                LocomotionState.Idle => "idle_" + (int)_direction,
                LocomotionState.Running => "running_" + (int)_direction,
                LocomotionState.Dying => "dying_" + (int)_direction,
                _ => _currentAnimationName
            };

            if (nextAnimation != _currentAnimationName)
            {
                PlayAnimation(nextAnimation);
            }
        }

        private void PlayAnimation(string name)
        {
            if (_animations.TryGetValue(name, out var seq))
            {
                var previous = _currentSequence;
                _currentSequence?.Stop();
                _currentAnimationName = name;
                _currentSequence = seq;
                if (previous != null)
                {
                    _currentSequence.AdoptPlaybackFrom(previous);
                }
                else
                {
                    _currentSequence.Play();
                }
            }
        }

        protected override void OnDestroy()
        {
            foreach (var sprite in _managedSprites)
            {
                if (sprite.Texture != null)
                {
                    sprite.Texture.Destroy();
                }
                TargetLayer?.Remove(sprite);
            }
            _managedSprites.Clear();
            _animations.Clear();
        }
    }
}
