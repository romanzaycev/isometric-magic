using System.Collections;
using System.Collections.Generic;
using IsometricMagic.Engine;
using IsometricMagic.Game.Animation;
using SDL2;

namespace IsometricMagic.Game.Scenes
{
    public class Animation : Scene
    {
        private readonly List<Sequence> _animations = new();
        private Sequence _currentSequence;
        private int _currentAnimationIndex;

        public Animation() : base("animation", true)
        {
        }

        protected override IEnumerator InitializeAsync()
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
                            Texture = tex
                        };
                        tex.LoadImage($"{framesPath}/{frameName}{i}{extension}");
                        
                        sprites.Add(sprite);
                        MainLayer.Add(sprite);
                    }

                    var seq = new Sequence(fullAnimationName, sprites);
                    seq.FrameDelay = 0.08f;
                    
                    _animations.Add(seq);
                }

                yield return true;
            }
            
            _currentSequence = _animations[_currentAnimationIndex];
            _currentSequence.Play();
        }

        public override void Update()
        {
            _currentSequence.Update(Application.DeltaTime);

            if (Input.IsPressed(SDL.SDL_Keycode.SDLK_SPACE))
            {
                _currentSequence.Stop();
                
                if (_currentAnimationIndex + 1 == _animations.Count)
                {
                    _currentAnimationIndex = 0;
                }
                else
                {
                    _currentAnimationIndex++;
                }

                _currentSequence = _animations[_currentAnimationIndex];
                _currentSequence.Play();
            }
        }
    }
}