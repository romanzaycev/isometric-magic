using System.Collections.Generic;
using System.Numerics;
using IsometricMagic.Engine;
using IsometricMagic.Game.Animation;
using IsometricMagic.Game.Model;

namespace IsometricMagic.Game.Character
{
    public enum HumanState
    {
        IDLE,
        RUNNING,
        DYING,
    }
    
    public class Human : WorldObject
    {
        private readonly Dictionary<string, Sequence> _animations = new();
        public Sequence CurrentSequence => _animations[_currentAnimation];

        private WorldDirection _direction = WorldDirection.N;
        public WorldDirection Direction
        {
            get => _direction;
            
            set {
                if (value != _direction)
                {
                    _state = State;
                    _direction = value;
                }
            }
        }
        private string _currentAnimation = "idle_0";
        private HumanState _state = HumanState.IDLE; 
        public HumanState State
        {
            get => _state;

            set
            {
                var nextAnimation = value switch
                {
                    HumanState.IDLE => "idle_" + (int) _direction,
                    HumanState.RUNNING => "running_" + (int) _direction,
                    HumanState.DYING => "dying_" + (int) _direction,
                    _ => _currentAnimation
                };

                if (nextAnimation != _currentAnimation)
                {
                    CurrentSequence.Stop();
                    _currentAnimation = nextAnimation;
                    CurrentSequence.Play();
                }

                _state = value;
            }
        }

        public Human(SceneLayer layer)
        {
            string[] availableAnimations =
            {
                "idle",
                "dying",
                "running",
            };
            string[] availableDirections =
            {
                "0",
                "45",
                "90",
                "135",
                "180",
                "225",
                "270",
                "315",
            };
            
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
                    List<Sprite> sprites = new();

                    for (int i = 0; i < totalFrames; i++)
                    {
                        var tex = new Texture(256, 256);
                        var sprite = new Sprite()
                        {
                            Width = 256,
                            Height = 256,
                            Texture = tex,
                            OriginPoint = OriginPoint.BottomCenter,
                            Sorting = 1000,
                            Transformation = {
                                Translate = new Vector2(0, -8),
                            }
                        };
                        tex.LoadImage(new AssetItem($"{framesPath}/{frameName}{i}{extension}"));
                        
                        sprites.Add(sprite);
                        layer.Add(sprite);
                    }

                    var seq = new Sequence(fullAnimationName, sprites)
                    {
                        FrameDelay = 0.08f
                    };

                    _animations.Add(fullAnimationName, seq);
                }
            }
            
            CurrentSequence.Play();
        }
    }
}