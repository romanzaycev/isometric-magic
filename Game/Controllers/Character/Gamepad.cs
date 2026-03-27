using System;
using IsometricMagic.Engine;
using IsometricMagic.Game.Model;

namespace IsometricMagic.Game.Controllers.Character
{
    public class Gamepad : CharacterMovementController
    {
        public override void HandleMovement(Game.Character.Character character, IsoWorldPositionConverter positionConverter)
        {
            var moveX = GetAxisMove(GamepadAxis.LeftX);
            var moveY = GetAxisMove(GamepadAxis.LeftY);

            ApplyDpad(ref moveX, ref moveY);

            if (moveX == 0 && moveY == 0)
            {
                StopMove(character);
                return;
            }

            TryMove(moveX, moveY, character, positionConverter);
        }

        private static int GetAxisMove(GamepadAxis axis)
        {
            var value = Input.GetAxis(axis);
            if (value == 0f)
            {
                return 0;
            }

            return (int)Math.Round(value * MAX_MOVE);
        }

        private static void ApplyDpad(ref int moveX, ref int moveY)
        {
            if (Input.IsDown(GamepadButton.DpadLeft))
            {
                moveX = -MAX_MOVE;
            }
            else if (Input.IsDown(GamepadButton.DpadRight))
            {
                moveX = MAX_MOVE;
            }

            if (Input.IsDown(GamepadButton.DpadUp))
            {
                moveY = -MAX_MOVE;
            }
            else if (Input.IsDown(GamepadButton.DpadDown))
            {
                moveY = MAX_MOVE;
            }
        }
    }
}
