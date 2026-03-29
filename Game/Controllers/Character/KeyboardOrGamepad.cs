using System;
using IsometricMagic.Engine;
using IsometricMagic.Game.Components;

namespace IsometricMagic.Game.Controllers.Character
{
    public class KeyboardOrGamepad : Component
    {
        private MotorComponent? _motor;

        protected override void Awake()
        {
            _motor = Entity?.GetComponent<MotorComponent>();
        }

        protected override void Update(float dt)
        {
            if (_motor == null) return;

            if (IsGamepadInputActive())
            {
                HandleGamepad();
                return;
            }

            HandleKeyboard();
        }

        private void HandleKeyboard()
        {
            var moveX = 0;
            var moveY = 0;

            if (Input.IsDown(Key.Up) || Input.IsDown(Key.W))
            {
                moveY = -_motor!.MaxMove;
            }

            if (Input.IsDown(Key.Down) || Input.IsDown(Key.S))
            {
                moveY = _motor!.MaxMove;
            }

            if (Input.IsDown(Key.Left) || Input.IsDown(Key.A))
            {
                moveX = -_motor!.MaxMove;
            }

            if (Input.IsDown(Key.Right) || Input.IsDown(Key.D))
            {
                moveX = _motor!.MaxMove;
            }

            _motor!.TryMove(moveX, moveY);
        }

        private void HandleGamepad()
        {
            var moveX = GetAxisMove(GamepadAxis.LeftX);
            var moveY = GetAxisMove(GamepadAxis.LeftY);

            ApplyDpad(ref moveX, ref moveY);

            if (moveX == 0 && moveY == 0)
            {
                _motor!.StopMove();
                return;
            }

            _motor!.TryMove(moveX, moveY);
        }

        private int GetAxisMove(GamepadAxis axis)
        {
            var value = Input.GetAxis(axis);
            if (value == 0f)
            {
                return 0;
            }

            return (int)Math.Round(value * _motor!.MaxMove);
        }

        private void ApplyDpad(ref int moveX, ref int moveY)
        {
            if (Input.IsDown(GamepadButton.DpadLeft))
            {
                moveX = -_motor!.MaxMove;
            }
            else if (Input.IsDown(GamepadButton.DpadRight))
            {
                moveX = _motor!.MaxMove;
            }

            if (Input.IsDown(GamepadButton.DpadUp))
            {
                moveY = -_motor!.MaxMove;
            }
            else if (Input.IsDown(GamepadButton.DpadDown))
            {
                moveY = _motor!.MaxMove;
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
