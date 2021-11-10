using IsometricMagic.Game.Model;

namespace IsometricMagic.Game.Controllers.Character
{
    public abstract class CharacterMovementController
    {
        public abstract void HandleMovement(Game.Character.Character character, IsoWorldPositionConverter positionConverter);
        
        protected static WorldDirection GetDirection(Game.Character.Character human, int moveX, int moveY)
        {
            if (moveX == 0 && moveY == 0)
            {
                return human.Direction;
            }

            if (moveY < 0 && moveX == 0)
            {
                return WorldDirection.SW;
            }

            if (moveY > 0 && moveX == 0)
            {
                return WorldDirection.NE;
            }
            
            if (moveY == 0 && moveX < 0)
            {
                return WorldDirection.NW;
            }

            if (moveY == 0 && moveX > 0)
            {
                return WorldDirection.SE;
            }

            // --
            
            if (moveY > 0 && moveX > 0)
            {
                return WorldDirection.E;
            }
            
            if (moveY > 0 && moveX < 0)
            {
                return WorldDirection.N;
            }
            
            if (moveY < 0 && moveX > 0)
            {
                return WorldDirection.S;
            }
            
            return WorldDirection.W;
        }
    }
}