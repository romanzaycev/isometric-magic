using System.Drawing;

namespace IsometricMagic.Engine
{
    class Camera
    {
        private Rectangle _rect = new Rectangle();
        public int ViewportWidth => _rect.Width;
        public int ViewportHeight => _rect.Height;
        public Rectangle Rect => _rect;

        public int X
        {
            get { return _rect.X; }

            set { _rect = new Rectangle(value, _rect.Y, _rect.Width, _rect.Height); }
        }

        public int Y
        {
            get { return _rect.Y; }

            set { _rect = new Rectangle(_rect.X, value, _rect.Width, _rect.Height); }
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