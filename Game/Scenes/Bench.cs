using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using IsometricMagic.Engine;
using IsometricMagic.Game.Controllers.Camera;

namespace IsometricMagic.Game.Scenes
{
    public class Bench : Scene
    {
        private const int SPRITES = 50000;
        private readonly Random _rand = new();
        private List<Sprite> _sprites = new();

        private float _spriteSpeed0 = 5f;
        private float _spriteSpeed1 = 7f;
        private float _spriteSpeed2 = 9f;
        
        private float _spriteRotation = 100f;
        private Dictionary<Sprite, Vector2> _startPos = new();
        private Dictionary<Sprite, float> _currAngle = new();

        public Bench() : base("bench", true)
        {
        }

        protected override IEnumerator InitializeAsync()
        {
            // Camera setup
            Camera.SetController(new MouseController());
            
            // Scene setup
            var tex = new Texture(64, 64);
            tex.LoadImage("./resources/data/textures/bear.jpeg");
            
            for (int i = 0; i < SPRITES; i++)
            {
                var sprite = new Sprite
                {
                    Texture = tex,
                    Position = new Vector2(_rand.Next(-2000, 2000), _rand.Next(-2000, 2000))
                };
            
                MainLayer.Add(sprite);
                _sprites.Add(sprite);
                _startPos.Add(sprite, sprite.Position);
                _currAngle.Add(sprite, 0);

                if (i % 250 == 0)
                {
                    yield return true;
                }
            }
        }

        protected override void DeInitialize()
        {
            _sprites.Clear();
            Camera.SetController(null);
        }

        public override void Update()
        {
            for (int i = 0; i < _sprites.Count; i++)
            {
                var sprite = _sprites[i];
                float currentAngle;
                float spriteSpeed;

                switch (i % 3)
                {
                    case 0:
                        spriteSpeed = _spriteSpeed0;
                        break;
                    
                    case 1:
                        spriteSpeed = _spriteSpeed1;
                        break;
                    
                    case 2:
                        spriteSpeed = _spriteSpeed2;
                        break;
                    
                    default:
                        spriteSpeed = _spriteSpeed0;
                        break;
                }
                
                if (i % 2 == 0)
                {
                    currentAngle = _currAngle[sprite] + spriteSpeed * Application.DeltaTime;
                }
                else
                {
                    currentAngle = _currAngle[sprite] - spriteSpeed * Application.DeltaTime;
                }

                Vector2 offset = new Vector2((float)Math.Sin(currentAngle), (float)Math.Cos(currentAngle)) * _spriteRotation;
                
                sprite.Position = _startPos[sprite] + offset;
                _currAngle[sprite] = currentAngle;
            }
        }
    }
}