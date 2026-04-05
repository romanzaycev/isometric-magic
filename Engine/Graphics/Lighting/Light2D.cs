using System.Numerics;
using IsometricMagic.Engine.Diagnostics;

namespace IsometricMagic.Engine.Graphics.Lighting
{
    [RuntimeEditorInspectable(EditableByDefault = false)]
    public class Light2D
    {
        [RuntimeEditorEditable(Step = 0.1)]
        public Vector2 Position;

        [RuntimeEditorEditable(Step = 0.05)]
        public Vector3 Color = new(1f, 1f, 1f);

        [RuntimeEditorEditable(Step = 0.1)]
        public float Intensity = 1f;

        [RuntimeEditorEditable(Step = 1)]
        public float Radius = 1024f;

        [RuntimeEditorEditable(Step = 0.1)]
        public float Height = 1.5f;

        [RuntimeEditorEditable(Step = 0.1)]
        public float Falloff = 2f;

        [RuntimeEditorEditable(Step = 1)]
        public float InnerRadius = 64f;

        [RuntimeEditorEditable(Step = 0.05)]
        public float CenterAttenuation = 0.75f;

        public Light2D(Vector2 position)
        {
            Position = position;
        }
    }
}
