using System;
using System.Numerics;
using IsometricMagic.Engine;
using SDL2;

namespace IsometricMagic.Game.Scenes
{
    public class Second : Scene
    {
        private bool _isDrag;
        private int _startMouseX;
        private int _startMouseY;
        private float _camSpeed = 1.2f;

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
                _startMouseX = Input.MouseX;
                _startMouseY = Input.MouseY;
                _isDrag = true;
            }

            if (Input.IsMouseReleased(SDL.SDL_BUTTON_LEFT) && _isDrag)
            {
                _startMouseX = 0;
                _startMouseY = 0;
                _isDrag = false;
            }

            if (_isDrag)
            {
                Camera.Rect.X += (int)((_startMouseX - Input.MouseX) * _camSpeed);
                Camera.Rect.Y += (int)((_startMouseY - Input.MouseY) * _camSpeed);
            }
            
            _startMouseX = Input.MouseX;
            _startMouseY = Input.MouseY;
        }
    }
}