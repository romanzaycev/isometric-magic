using System;
using System.Numerics;

namespace IsometricMagic.Engine
{
    public enum PivotMode
    {
        Centered,
    }

    public enum OriginPoint
    {
        LeftTop,
        LeftCenter,
        LeftBottom,
        Centered,
        RightTop,
        RightCenter,
        RightBottom,
        TopCenter,
        BottomCenter,
    }

    public class Rotation
    {
        public bool Clockwise = true;

        private double _angle;
        
        /**
         * Normalized rotation angle when 1.f == 360 degrees
         */
        public double Angle
        {
            get => _angle;
            set => _angle = MathHelper.NormalizeNor(value);
        }
    }

    public class Transformation
    {
        public Rotation Rotation { get; } = new();
    }

    public class Sprite
    {
        private static readonly SpriteHolder SpriteHolder = SpriteHolder.GetInstance();
        
        public string Name = string.Empty;
        
        public Vector2 Position = Vector2.Zero;
        public Vector2 Origin = Vector2.Zero;
        public Vector2 Pivot = Vector2.Zero;
        
        public PivotMode PivotMode = PivotMode.Centered;
        public OriginPoint OriginPoint = OriginPoint.LeftTop;

        public int Width;
        public int Height;

        private Texture _texture; 

        public Texture Texture
        {
            get => _texture;

            set
            {
                if (Width == 0 && Height == 0)
                {
                    Width = value.Width;
                    Height = value.Height;
                }

                _texture = value;
            }
        }
        
        public bool Visible = true;
        
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

        private Transformation _transformation = new();
        public Transformation Transformation => _transformation;

        public Sprite(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public Sprite()
        {
        }
    }
}