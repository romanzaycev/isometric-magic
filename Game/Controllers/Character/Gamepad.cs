using System;
using IsometricMagic.Engine;
using IsometricMagic.Game.Components.Actor;

namespace IsometricMagic.Game.Controllers.Character
{
    public class Gamepad : Component
    {
        private MotorComponent? _motor;

        protected override void Awake()
        {
            _motor = Entity?.GetComponent<MotorComponent>();
        }

        protected override void Update(float dt)
        {
            if (_motor == null) return;

            var moveX = GetAxisMove(GamepadAxis.LeftX);
            var moveY = GetAxisMove(GamepadAxis.LeftY);

            ApplyDpad(ref moveX, ref moveY);

            if (moveX == 0 && moveY == 0)
            {
                _motor.StopMove();
                return;
            }

            _motor.TryMove(moveX, moveY);
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
    }
}
