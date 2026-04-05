using System.Numerics;
using IsometricMagic.Engine.Core.Rendering;
using IsometricMagic.Engine.Assets;
using IsometricMagic.Engine.Diagnostics;
using IsometricMagic.Engine.Graphics.Materials;

namespace IsometricMagic.Engine.Rendering
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

    public enum SpriteBlendMode
    {
        Normal,
        Multiply,
        Screen,
        SoftLight,
        Overlay,
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
        public Vector2 Translate = Vector2.Zero;
    }

    [RuntimeEditorInspectable(EditableByDefault = false)]
    public class Sprite
    {
        private static readonly SpriteHolder SpriteHolder = SpriteHolder.GetInstance();
        
        [RuntimeEditorEditable]
        public string Name = string.Empty;
        
        [RuntimeEditorEditable(Step = 0.1)]
        public Vector2 Position = Vector2.Zero;

        [RuntimeEditorEditable(Step = 0.1)]
        public Vector2 Origin = Vector2.Zero;

        [RuntimeEditorEditable(Step = 0.1)]
        public Vector2 Pivot = Vector2.Zero;

        [RuntimeEditorEditable(Step = 0.05)]
        public Vector4 Color = new(1f, 1f, 1f, 1f);

        [RuntimeEditorEditable]
        public SpriteBlendMode BlendMode = SpriteBlendMode.Normal;
        
        [RuntimeEditorEditable]
        public PivotMode PivotMode = PivotMode.Centered;

        [RuntimeEditorEditable]
        public OriginPoint OriginPoint = OriginPoint.LeftTop;

        [RuntimeEditorEditable(Step = 1)]
        public int Width;

        [RuntimeEditorEditable(Step = 1)]
        public int Height;

        private Texture _texture = null!; 
        public Texture? NormalMap;
        public IMaterial? Material;
        public SpriteOutline Outline { get; } = new();

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
        
        [RuntimeEditorEditable]
        public bool Visible = true;
        
        private int _sorting = 0;
        public int Sorting
        {
            get => _sorting;

            set
            {
                if (value == _sorting) return;
                var oldSorting = _sorting;
                _sorting = value;
                SpriteHolder.TrySetReindex(this, oldSorting, value);
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
