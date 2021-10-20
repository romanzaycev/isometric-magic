using System.Collections.Generic;

namespace IsometricMagic.Engine
{
    class SpriteHolder
    {
        private static readonly SpriteHolder Instance = new();
        private readonly List<Sprite> _list = new();
        private readonly Dictionary<string, List<Sprite>> _tags = new();

        public static SpriteHolder GetInstance()
        {
            return Instance;
        }

        public void Add(Sprite sprite)
        {
            if (!_list.Contains(sprite))
            {
                _list.Add(sprite);
            }
        }

        public void Add(Sprite sprite, string tag0)
        {
            if (!_list.Contains(sprite))
            {
                _list.Add(sprite);
            }

            if (!_tags.ContainsKey(tag0))
            {
                _tags[tag0] = new();
            }

            if (_tags[tag0].Contains(sprite))
            {
                _tags[tag0].Add(sprite);
            }
        }

        public Sprite[] GetSprites()
        {
            return _list.ToArray();
        }

        public void Remove(Sprite sprite)
        {
            _list.Remove(sprite);

            foreach (var tag in _tags)
            {
                tag.Value.Remove(sprite);
            }
        }
    }
}