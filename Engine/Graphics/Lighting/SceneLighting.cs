using System.Collections.Generic;
using System.Numerics;

namespace IsometricMagic.Engine.Graphics.Lighting
{
    public class SceneLighting
    {
        private readonly List<Light2D> _lights = new();
        public IReadOnlyList<Light2D> Lights => _lights;

        public Vector3 AmbientColor = new(1f, 1f, 1f);
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
