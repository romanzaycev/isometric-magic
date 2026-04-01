using System.Drawing;
using System.Numerics;

namespace IsometricMagic.Engine.Rendering
{
    public class Camera
    {
        public Rectangle Rect;
        public Vector2 SubpixelOffset = Vector2.Zero;

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

        public Vector2 GetCanvasPosition(int mouseX, int mouseY)
        {
            return new Vector2(
                mouseX + Rect.Left + SubpixelOffset.X,
                mouseY + Rect.Top + SubpixelOffset.Y
            );
        }
    }
}
