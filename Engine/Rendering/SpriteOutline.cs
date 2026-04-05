using System.Numerics;
using IsometricMagic.Engine.Diagnostics;

namespace IsometricMagic.Engine.Rendering
{
    public enum OutlineLayering
    {
        Under,
        Over
    }

    [RuntimeEditorInspectable(EditableByDefault = false)]
    public sealed class SpriteOutline
    {
        [RuntimeEditorEditable]
        public bool Enabled;

        [RuntimeEditorEditable(Step = 0.1)]
        public float ThicknessTexels = 1f;

        [RuntimeEditorEditable(Step = 0.05)]
        public Vector4 Color = new(1f, 1f, 1f, 1f);

        [RuntimeEditorEditable]
        public OutlineLayering Layering = OutlineLayering.Under;

        [RuntimeEditorEditable]
        public bool ForceAlphaBlend;
    }
}
