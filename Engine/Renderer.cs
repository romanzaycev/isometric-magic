using System.Drawing;
using IsometricMagic.Engine.Graphics;

namespace IsometricMagic.Engine
{
    public class Renderer
    {
        private readonly IGraphics _graphics;
        private static readonly Application Application = Application.GetInstance();
        private static readonly TextureHolder TextureHolder = TextureHolder.GetInstance();
        private readonly Camera _camera;

        public Renderer(IGraphics graphics)
        {
            _graphics = graphics;
            _camera = new Camera(
                Application.GetConfig().WindowWidth,
                Application.GetConfig().WindowHeight
            );
        }

        public void DrawAll()
        {
            _graphics.Draw(SceneManager.GetInstance().GetCurrent(), _camera);
        }

        public Camera GetCamera()
        {
            return _camera;
        }

        public void HandleWindowResized(int w, int h)
        {
            _camera.Rect = new Rectangle()
            {
                Width = w,
                Height = h,
                X = _camera.Rect.X,
                Y = _camera.Rect.Y
            };
        }
    }
}