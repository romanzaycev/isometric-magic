using System.Numerics;
using IsometricMagic.Engine;

namespace IsometricMagic.Game.Controllers.Camera
{
    public class LookAtController : ICameraController
    {
        private Vector2 _pos;

        public void SetPos(Vector2 pos)
        {
            _pos = pos;
        }
        
        public void UpdateCamera(Engine.Camera camera)
        {
            if (_pos != Vector2.Zero)
            {
                var cameraRect = camera.Rect;
                var nextX = (int) _pos.X - cameraRect.Width / 2;
                var nextY = (int) _pos.Y - cameraRect.Height / 2;

                if (nextX >= -200)
                {
                    camera.Rect.X = nextX;
                }

                if (nextY >= -200)
                {
                    camera.Rect.Y = nextY;
                }
            }
        }
    }
}