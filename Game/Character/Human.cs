using System.Collections.Generic;
using System.Numerics;
using IsometricMagic.Engine;
using IsometricMagic.Game.Animation;

namespace IsometricMagic.Game.Character
{
    public class Human : Character
    {
        public Human(SceneLayer layer)
        {
            string[] availableAnimations =
            {
                "idle",
                "dying",
                "running"
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
                "315"
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

                    for (var i = 0; i < totalFrames; i++)
                    {
                        var tex = new Texture(256, 256);
                        var sprite = new Sprite
                        {
                            Width = 256,
                            Height = 256,
                            Texture = tex,
                            OriginPoint = OriginPoint.BottomCenter,
                            Sorting = 1000,
                            Transformation = {
                                Translate = new Vector2(0, 8),
                            }
                        };
                        tex.LoadImage($"{framesPath}/{frameName}{i}{extension}");
                        
                        sprites.Add(sprite);
                        layer.Add(sprite);
                    }

                    var seq = new Sequence(fullAnimationName, sprites)
                    {
                        FrameDelay = 0.08f
                    };

                    Animations.Add(fullAnimationName, seq);
                }
            }
            
            CurrentSequence.Play();
        }
    }
}