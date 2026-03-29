using IsometricMagic.Engine;
using IsometricMagic.Game.Components.Actor;

namespace IsometricMagic.Game.Controllers.Character
{
    public class Mouse : Component
    {
        private static readonly Engine.Camera Camera = Application.GetInstance().GetRenderer().GetCamera();
        private bool _isDrag;
        private int _startMouseX;
        private int _startMouseY;

        private MotorComponent? _motor;

        protected override void Awake()
        {
            _motor = Entity?.GetComponent<MotorComponent>();
        }

        protected override void Update(float dt)
        {
            if (_motor == null) return;

            if (Input.WasPressed(MouseButton.Left) && !_isDrag)
            {
                _startMouseX = Input.MouseX;
                _startMouseY = Input.MouseY;
                _isDrag = true;
            }

            if (Input.WasReleased(MouseButton.Left) && _isDrag)
            {
                _isDrag = false;
            }

            if (_isDrag)
            {
                var canvasMousePos = Camera.GetCanvasPosition(_startMouseX, _startMouseY);
                var positionConverter = _motor.Converter;
                var position = _motor.PositionComponent;
                if (positionConverter == null || position == null)
                {
                    _motor.StopMove();
                    return;
                }

                var mouseWorldPos = positionConverter.GetWorldPosition((int)canvasMousePos.X, (int)canvasMousePos.Y);

                var moveX = (int)(mouseWorldPos.X - position.WorldPosX);
                var moveY = (int)(mouseWorldPos.Y - position.WorldPosY);
                _motor.TryMove(moveX, moveY);
            }
            else
            {
                _motor.StopMove();
            }

            _startMouseX = Input.MouseX;
            _startMouseY = Input.MouseY;
        }
    }
}
