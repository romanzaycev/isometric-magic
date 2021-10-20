using System;
using System.Collections.Generic;
using System.Numerics;
using IsometricMagic.Engine;
using SDL2;

namespace IsometricMagic.Game.Scenes
{
    public class Main : Scene
    {
        private List<Sprite> _sprites = new();
        private int _activeSpriteIndex;
        private bool _isSwitchPressed;

        public Main() : base("main")
        {
        }

        protected override void Initialize()
        {
            var tex = new Texture(900, 900);
            tex.LoadImage(new AssetItem("./resources/data/textures/thonk.jpeg"));

            for (var i = 0; i < 3; i++)
            {
                var sprite = new Sprite
                {
                    Texture = tex,
                    Name = $"Sprite{i + 1}",
                    Position = new Vector2(100 * i, 100 * i)
                };
                
                MainLayer.Add(sprite);
                _sprites.Add(sprite);
            }

            Console.WriteLine($"Scene {Name} initialized");
        }

        public override void Update()
        {
            if (Input.IsPressed(SDL.SDL_Keycode.SDLK_SPACE) && !_isSwitchPressed)
            {
                var nIndex = _activeSpriteIndex + 1;

                if (_sprites.Count < nIndex + 1)
                {
                    nIndex = 0;
                }
                
                _activeSpriteIndex = nIndex;
                _isSwitchPressed = true;
            }

            if (Input.IsReleased(SDL.SDL_Keycode.SDLK_SPACE) && _isSwitchPressed)
            {
                _isSwitchPressed = false;
            }

            UpdateActiveSprite();
            
            if (Input.IsPressed(SDL.SDL_Keycode.SDLK_LEFT))
            {
                Camera.X += 10;
            }
            
            if (Input.IsPressed(SDL.SDL_Keycode.SDLK_RIGHT))
            {
                Camera.X -= 10;
            }
            
            if (Input.IsPressed(SDL.SDL_Keycode.SDLK_UP))
            {
                Camera.Y += 10;
            }

            if (Input.IsPressed(SDL.SDL_Keycode.SDLK_DOWN))
            {
                Camera.Y -= 10;
            }

            if (Input.IsPressed(SDL.SDL_Keycode.SDLK_0))
            {
                SceneManager.LoadByName("second");
            }
        }

        private void UpdateActiveSprite()
        {
            for (var i = 0; i < _sprites.Count; i++)
            {
                _sprites[i].Sorting = _activeSpriteIndex == i ? 1 : 0;
            }
        }
    }
}