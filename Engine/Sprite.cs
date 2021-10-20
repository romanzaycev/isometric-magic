using System;
using System.Numerics;

namespace IsometricMagic.Engine
{
    public class Sprite
    {
        public string Name = String.Empty;
        public Vector2 Position = Vector2.Zero;
        public Vector2 Pivot = Vector2.Zero;
        public Texture Texture = null;
        public int Sorting = 0;
        public bool Visible = true;
    }
}