using IsometricMagic.Engine;

namespace IsometricMagic.Game.Controllers.Camera
{
    public class MouseController: ICameraController
    {
        private bool _isDrag;
        private int _startMouseX;
        private int _startMouseY;

        public void UpdateCamera(Engine.Camera camera)
        {
            if (Input.WasPressed(MouseButton.Left) && !_isDrag)
            {
                _startMouseX = Input.MouseX;
                _startMouseY = Input.MouseY;
                _isDrag = true;
            }

            if (Input.WasReleased(MouseButton.Left) && _isDrag)
            {
                _startMouseX = 0;
                _startMouseY = 0;
                _isDrag = false;
            }

            if (_isDrag)
            {
                camera.Rect.X -= _startMouseX - Input.MouseX;
                camera.Rect.Y -= _startMouseY - Input.MouseY;
            }
            
            _startMouseX = Input.MouseX;
            _startMouseY = Input.MouseY;
        }
    }
}