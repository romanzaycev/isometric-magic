using System;
using System.Numerics;
using IsometricMagic.Engine;
using SDL2;

namespace IsometricMagic.Game.Scenes
{
    public class Second : Scene
    {
        private bool _isDrag;
        
        public Second() : base("second")
        {
        }

        protected override void Initialize()
        {
            var tex = new Texture(900, 900);
            tex.LoadImage(new AssetItem("./resources/data/textures/thonk.jpeg"));

            var sprite = new Sprite
            {
                Texture = tex,
                Position = new Vector2(500, 500)
            };
            
            MainLayer.Add(sprite);

            Console.WriteLine($"Scene {Name} initialized");
        }

        public override void Update()
        {
            if (Input.IsMousePressed(SDL.SDL_BUTTON_LEFT) && !_isDrag)
            {
                _isDrag = true;
            }

            if (Input.IsMouseReleased(SDL.SDL_BUTTON_LEFT) && _isDrag)
            {
                _isDrag = false;
            }

            if (_isDrag)
            {
                Camera.X = Input.MouseX - Camera.ViewportWidth / 2;
                Camera.Y = Input.MouseY - Camera.ViewportHeight / 2;
            }
        }
    }
}