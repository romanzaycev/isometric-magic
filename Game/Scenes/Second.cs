using System;
using System.Numerics;
using IsometricMagic.Engine;
using IsometricMagic.Game.Controllers.Camera;

namespace IsometricMagic.Game.Scenes
{
    public class Second : Scene
    {
        public Second() : base("second")
        {
        }

        protected override void Initialize()
        {
            Camera.SetController(new MouseController());

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

        protected override void DeInitialize()
        {
            Camera.SetController(null);
        }
    }
}