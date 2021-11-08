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
        private readonly IsoWorldPositionConverter _converter;
        private readonly Dictionary<string, Sequence> _animations = new();
        public Sequence CurrentSequence
        {
            get
            {
                if (_animations.ContainsKey(_currentAnimation))
                {
                    return _animations[_currentAnimation]; 
                }

                return null;
            }
        }

        private string _currentAnimation = "idle_0";
        private HumanState _state = HumanState.IDLE;

        public Human(SceneLayer layer, IsoWorldPositionConverter converter)
        {
            _converter = converter;
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

        public override Vector2 GetScreenPosition()
        {
            return _converter.GetCanvasPosition(this);
        }
    }
}