using IsometricMagic.Engine;
using IsometricMagic.Game.Model;

namespace IsometricMagic.Game.Controllers.Character
{
    public class Keyboard : CharacterMovementController
    {
        public override void HandleMovement(Game.Character.Character character, IsoWorldPositionConverter positionConverter)
        {
            var moveX = 0;
            var moveY = 0;
            
            if (Input.IsDown(Key.Up))
            {
                moveY = -5;
            }

            if (Input.IsDown(Key.Down))
            {
                moveY = 5;
            }
            
            if (Input.IsDown(Key.Left))
            {
                moveX = -5;
            }
            
            if (Input.IsDown(Key.Right))
            {
                moveX = 5;
            }

            TryMove(moveX, moveY, character, positionConverter);
        }
    }
}