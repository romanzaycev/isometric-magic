using System;
using System.Numerics;

namespace IsometricMagic.Engine
{
    public class Sprite
    {
        private static readonly SpriteHolder SpriteHolder = SpriteHolder.GetInstance();
        
        public string Name = String.Empty;
        public Vector2 Position = Vector2.Zero;
        public Vector2 Pivot = Vector2.Zero;
        public Texture Texture = null;
        private int _sorting = 0;

        public int Sorting
        {
            get => _sorting;

            set
            {
                if (value != _sorting)
                {
                    SpriteHolder.TrySetReindex(this);
                }

                _sorting = value;
            }
        }
        
        public bool Visible = true;
    }
}