using IsometricMagic.Engine;

namespace IsometricMagic.Game.Controllers.Camera
{
    public class KeyboardArrowsController : ICameraController
    {
        public void UpdateCamera(Engine.Camera camera)
        {
            if (Input.IsDown(Key.Left))
            {
                camera.Rect.X += 10;
            }
            
            if (Input.IsDown(Key.Right))
            {
                camera.Rect.X -= 10;
            }
            
            if (Input.IsDown(Key.Up))
            {
                camera.Rect.Y += 10;
            }

            if (Input.IsDown(Key.Down))
            {
                camera.Rect.Y -= 10;
            }
        }
    }
}