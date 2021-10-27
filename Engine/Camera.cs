using System.Drawing;

namespace IsometricMagic.Engine
{
    public class Camera
    {
        public Rectangle Rect;
        public ICameraController Controller { get; private set; }

        public Camera(int viewportWidth, int viewportHeight)
        {
            Rect = new Rectangle
            {
                Width = viewportWidth,
                Height = viewportHeight,
                X = 0,
                Y = 0
            };
        }

        public void SetController(ICameraController controller)
        {
            Controller = controller;
        }
    }
}