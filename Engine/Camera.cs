using System.Drawing;

namespace IsometricMagic.Engine
{
    public class Camera
    {
        private Rectangle _rect;
        public int ViewportWidth => _rect.Width;
        public int ViewportHeight => _rect.Height;
        public Rectangle Rect => _rect;

        public int X
        {
            get => _rect.X;

            set => _rect = new Rectangle(value, _rect.Y, _rect.Width, _rect.Height);
        }

        public int Y
        {
            get => _rect.Y;

            set => _rect = new Rectangle(_rect.X, value, _rect.Width, _rect.Height);
        }

        public int H
        {
            set => _rect = new Rectangle(_rect.X, _rect.Y, _rect.Width, value);
        }
        
        public int W
        {
            set => _rect = new Rectangle(_rect.X, _rect.Y, value, _rect.Width);
        }
        
        public Camera(int viewportWidth, int viewportHeight)
        {
            _rect.Width = viewportWidth;
            _rect.Height = viewportHeight;
            _rect.X = 0;
            _rect.Y = 0;
        }
    }
}