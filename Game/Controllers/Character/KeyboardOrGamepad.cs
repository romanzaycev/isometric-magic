using System;
using IsometricMagic.Engine;
using IsometricMagic.Game.Model;

namespace IsometricMagic.Game.Controllers.Character
{
    public class KeyboardOrGamepad : CharacterMovementController
    {
        private readonly Keyboard _keyboard = new();
        private readonly Gamepad _gamepad = new();

        public override void HandleMovement(Game.Character.Character character, IsoWorldPositionConverter positionConverter)
        {
            if (IsGamepadInputActive())
            {
                _gamepad.HandleMovement(character, positionConverter);
                return;
            }

            _keyboard.HandleMovement(character, positionConverter);
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
