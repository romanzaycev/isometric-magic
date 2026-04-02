using IsometricMagic.Game.Components.Actor;

namespace IsometricMagic.Game.Controllers.Character
{
    public class KeyboardOrGamepad : Component
    {
        private MotorComponent? _motor;

        public override ComponentUpdateGroup UpdateGroup => ComponentUpdateGroup.Critical;

        public override int UpdateOrder => 10;

        protected override void Awake()
        {
            _motor = Entity?.GetComponent<MotorComponent>();
        }

        protected override void Update(float dt)
        {
            if (_motor == null) return;

            if (IsGamepadInputActive())
            {
                HandleGamepad(dt);
                return;
            }

            HandleKeyboard(dt);
        }

        private void HandleKeyboard(float dt)
        {
            var moveX = 0f;
            var moveY = 0f;

            if (Input.IsDown(Key.Up) || Input.IsDown(Key.W))
            {
                moveY = -1f;
            }

            if (Input.IsDown(Key.Down) || Input.IsDown(Key.S))
            {
                moveY = 1f;
            }

            if (Input.IsDown(Key.Left) || Input.IsDown(Key.A))
            {
                moveX = -1f;
            }

            if (Input.IsDown(Key.Right) || Input.IsDown(Key.D))
            {
                moveX = 1f;
            }

            _motor!.TryMoveInput(moveX, moveY, dt);
        }

        private void HandleGamepad(float dt)
        {
            var moveX = GetAxisMove(GamepadAxis.LeftX);
            var moveY = GetAxisMove(GamepadAxis.LeftY);

            ApplyDpad(ref moveX, ref moveY);

            if (moveX == 0 && moveY == 0)
            {
                _motor!.StopMove();
                return;
            }

            _motor!.TryMoveInput(moveX, moveY, dt);
        }

        private static float GetAxisMove(GamepadAxis axis)
        {
            var value = Input.GetAxis(axis);
            if (Math.Abs(value) < float.Epsilon)
            {
                return 0;
            }

            return value;
        }

        private void ApplyDpad(ref float moveX, ref float moveY)
        {
            if (Input.IsDown(GamepadButton.DpadLeft))
            {
                moveX = -1f;
            }
            else if (Input.IsDown(GamepadButton.DpadRight))
            {
                moveX = 1f;
            }

            if (Input.IsDown(GamepadButton.DpadUp))
            {
                moveY = -1f;
            }
            else if (Input.IsDown(GamepadButton.DpadDown))
            {
                moveY = 1f;
            }
        }

        private static bool IsGamepadInputActive()
        {
            if (!Input.IsGamepadConnected)
            {
                return false;
            }

            if (Input.IsDown(GamepadButton.DpadUp) || Input.IsDown(GamepadButton.DpadDown)
                || Input.IsDown(GamepadButton.DpadLeft) || Input.IsDown(GamepadButton.DpadRight))
            {
                return true;
            }

            var axisX = Input.GetAxis(GamepadAxis.LeftX);
            var axisY = Input.GetAxis(GamepadAxis.LeftY);
            return Math.Abs(axisX) > 0f || Math.Abs(axisY) > 0f;
        }
    }
}
