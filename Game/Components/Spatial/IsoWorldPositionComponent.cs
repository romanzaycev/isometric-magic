using IsometricMagic.Game.Model;

namespace IsometricMagic.Game.Components.Spatial
{
    public class IsoWorldPositionComponent : Component
    {
        public IsoWorldPosition Position = new(0f, 0f);

        [RuntimeEditorEditable]
        public float X
        {
            get => Position.X;
            set => Position = Position with { X = value };
        }

        [RuntimeEditorEditable]
        public float Y
        {
            get => Position.Y;
            set => Position = Position with { Y = value };
        }
    }
}
