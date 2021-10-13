using System;
using System.Numerics;

namespace IsometricMagic.Engine
{
    class Sprite
    {
        private static readonly SpriteHolder SpriteHolder = SpriteHolder.GetInstance();
        private static readonly TextureHolder TextureHolder = TextureHolder.GetInstance();
        
        public string Name = String.Empty;
        public Vector2 Position = Vector2.Zero;
        public Vector2 Pivot = Vector2.Zero;
        public Texture Texture = null;
        public int Sorting = 0;

        public Sprite()
        {
            SpriteHolder.PushSprite(this);
        }

        public void Destroy()
        {
            SpriteHolder.Remove(this);
        }
    }
}