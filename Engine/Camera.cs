using System.Drawing;
using System.Numerics;

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

        public Vector2 GetCanvasPosition(int mouseX, int mouseY)
        {
            return new Vector2(
                mouseX + Rect.Left,
                mouseY + Rect.Top
            );
        }
    }
}