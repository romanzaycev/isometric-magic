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
            if (_pos != null)
            {
                var cameraRect = camera.Rect;

                camera.Rect.X = -(int) _pos.X + cameraRect.Width / 2;
                camera.Rect.Y = -(int) _pos.Y + cameraRect.Height / 2;
            }
        }
    }
}