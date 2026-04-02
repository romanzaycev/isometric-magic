using IsometricMagic.Game.Components.Actor;

namespace IsometricMagic.Game.Controllers.Character
{
    public class Gamepad : Component
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

            var moveX = GetAxisMove(GamepadAxis.LeftX);
            var moveY = GetAxisMove(GamepadAxis.LeftY);

            ApplyDpad(ref moveX, ref moveY);

            if (moveX == 0 && moveY == 0)
            {
                _motor.StopMove();
                return;
            }

            _motor.TryMoveInput(moveX, moveY, dt);
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

        private static void ApplyDpad(ref float moveX, ref float moveY)
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
    }
}
