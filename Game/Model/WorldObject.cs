using System.Numerics;

namespace IsometricMagic.Game.Model
{
    public abstract class WorldObject
    {
        public int WorldPosX;
        public int WorldPosY;

        public abstract Vector2 GetScreenPosition();
    }
}