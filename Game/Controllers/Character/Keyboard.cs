using IsometricMagic.Engine;
using IsometricMagic.Game.Components;

namespace IsometricMagic.Game.Controllers.Character
{
    public class Keyboard : Component
    {
        private MotorComponent? _motor;

        protected override void Awake()
        {
            _motor = Entity?.GetComponent<MotorComponent>();
        }

        protected override void Update(float dt)
        {
            if (_motor == null) return;

            var moveX = 0;
            var moveY = 0;

            if (Input.IsDown(Key.Up) || Input.IsDown(Key.W))
            {
                moveY = -_motor.MaxMove;
            }

            if (Input.IsDown(Key.Down) || Input.IsDown(Key.S))
            {
                moveY = _motor.MaxMove;
            }

            if (Input.IsDown(Key.Left) || Input.IsDown(Key.A))
            {
                moveX = -_motor.MaxMove;
            }

            if (Input.IsDown(Key.Right) || Input.IsDown(Key.D))
            {
                moveX = _motor.MaxMove;
            }

            _motor.TryMove(moveX, moveY);
        }
    }
}
