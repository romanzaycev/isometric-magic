using IsometricMagic.Engine;
using IsometricMagic.Game.Components.Actor;

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

            _motor.TryMoveInput(moveX, moveY, dt);
        }
    }
}
