using System.Collections.Generic;

namespace IsometricMagic.Engine.Graphics.Effects
{
    public class PostProcessStack
    {
        private readonly List<IPostProcessEffect> _effects = new();
        public bool Enabled { get; set; } = true;

        public IReadOnlyList<IPostProcessEffect> Effects => _effects;

        public void Add(IPostProcessEffect effect)
        {
            if (!_effects.Contains(effect))
            {
                _effects.Add(effect);
            }
        }

        public void Remove(IPostProcessEffect effect)
        {
            _effects.Remove(effect);
        }

        public void Clear()
        {
            _effects.Clear();
        }
    }
}
