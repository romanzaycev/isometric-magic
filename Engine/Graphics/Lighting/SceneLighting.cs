using System.Collections.Generic;
using System.Numerics;
using IsometricMagic.Engine.Diagnostics;

namespace IsometricMagic.Engine.Graphics.Lighting
{
    [RuntimeEditorInspectable(EditableByDefault = false)]
    public class SceneLighting
    {
        private readonly List<Light2D> _lights = new();
        public IReadOnlyList<Light2D> Lights => _lights;

        [RuntimeEditorEditable(Step = 0.05)]
        public Vector3 AmbientColor = new(1f, 1f, 1f);

        [RuntimeEditorEditable(Step = 0.05)]
        public float AmbientIntensity = 0.45f;

        public void Add(Light2D light)
        {
            if (!_lights.Contains(light))
            {
                _lights.Add(light);
            }
        }

        public void Remove(Light2D light)
        {
            _lights.Remove(light);
        }

        public void Clear()
        {
            _lights.Clear();
        }
    }
}
