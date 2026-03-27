using System;
using System.Collections.Generic;
using System.Numerics;
using IsometricMagic.Engine;
using IsometricMagic.Game.Controllers.Camera;

namespace IsometricMagic.Game.Scenes
{
    public class Main : Scene
    {
        private readonly List<Sprite> _sprites = new();
        private int _activeSpriteIndex;

        public Main() : base("main")
        {
        }

        protected override void Initialize()
        {
            // Camera setup
            Camera.SetController(new KeyboardArrowsController());
            
            // Scene setup
            var tex = new Texture(900, 900);
            tex.LoadImage("./resources/data/textures/thonk.jpeg");

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
            if (Input.WasPressed(Key.Space))
            {
                var nIndex = _activeSpriteIndex + 1;

                if (_sprites.Count < nIndex + 1)
                {
                    nIndex = 0;
                }
                
                _activeSpriteIndex = nIndex;
            }

            UpdateActiveSprite();

            // Switch scenes
            if (Input.WasPressed(Key.Num0))
            {
                SceneManager.LoadByName("second");
            }
            
            if (Input.WasPressed(Key.Num9))
            {
                SceneManager.LoadByName("bench");
            }
            
            if (Input.WasPressed(Key.Num8))
            {
                SceneManager.LoadByName("iso_test");
            }
        }

        protected override void DeInitialize()
        {
            Camera.SetController(null);
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