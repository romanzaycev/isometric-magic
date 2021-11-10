using System;
using System.Numerics;
using IsometricMagic.Engine;

namespace IsometricMagic.Game.Scenes
{
    public class Loading : Scene
    {
        private Sprite _loadingText;
        private Sprite _loadingCircle;
        private float _loadingCircleSpeed = 0.3f;
        private double _currLoadingCircleAngle;
        
        public Loading() : base("loading")
        {
        }

        protected override void Initialize()
        {
            Camera.Rect.X = 0;
            Camera.Rect.Y = 0;
            
            var textTex = new Texture(288, 49);
            textTex.LoadImage(new AssetItem("./resources/data/textures/loading_text.png"));

            _loadingText = new Sprite
            {
                Texture = textTex,
                Name = "Loading text",
                OriginPoint = OriginPoint.Centered
            };
            
            UiLayer.Add(_loadingText);

            var circleTex = new Texture(128, 128);
            circleTex.LoadImage(new AssetItem("./resources/data/textures/loading_circle.png"));

            _loadingCircle = new Sprite
            {
                Texture = circleTex,
                Name = "Loading circle",
                OriginPoint = OriginPoint.RightBottom,
            };
            
            UiLayer.Add(_loadingCircle);
            
            Console.WriteLine($"Scene {Name} initialized");
        }

        public override void Update()
        {
            _loadingText.Position = new Vector2
            {
                X = Camera.Rect.Width / 2,
                Y = Camera.Rect.Height / 2
            };
            
            _loadingCircle.Position = new Vector2
            {
                X = Application.ViewportWidth - 40,
                Y = Application.ViewportHeight - 40
            };
            _currLoadingCircleAngle += _loadingCircleSpeed * Application.DeltaTime;
            _loadingCircle.Transformation.Rotation.Angle += _currLoadingCircleAngle;
            _currLoadingCircleAngle = 0;
        }
    }
}