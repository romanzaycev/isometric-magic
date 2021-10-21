using System.Collections.Generic;

namespace IsometricMagic.Engine
{
    class SpriteHolder
    {
        private static readonly SpriteHolder Instance = new();
        private readonly Dictionary<string, List<Sprite>> _tags = new();
        private readonly Dictionary<string, bool> _isChanged = new();
        private readonly Dictionary<Sprite, List<string>> _tagIndex = new();

        public static SpriteHolder GetInstance()
        {
            return Instance;
        }

        public void Add(Sprite sprite, string tag0)
        {
            if (!_tags.ContainsKey(tag0))
            {
                _tags.Add(tag0, new List<Sprite>());
            }

            if (!_tags[tag0].Contains(sprite))
            {
                _tags[tag0].Add(sprite);
            }

            if (!_isChanged.ContainsKey(tag0))
            {
                _isChanged.Add(tag0, false);
            }

            _isChanged[tag0] = true;

            if (!_tagIndex.ContainsKey(sprite))
            {
                _tagIndex[sprite] = new List<string>();
            }
            
            _tagIndex[sprite].Add(tag0);
        }

        public IReadOnlyList<Sprite> GetSprites(string tag0)
        {
            if (!_tags.ContainsKey(tag0)) return new List<Sprite>(); // @TODO Return empty IReadOnlyList?
            if (!_isChanged.ContainsKey(tag0) || !_isChanged[tag0]) return _tags[tag0];
                
            _tags[tag0].Sort((spriteA, spriteB) => spriteA.Sorting.CompareTo(spriteB.Sorting));
            _isChanged[tag0] = false;

            return _tags[tag0];
        }

        public void Remove(Sprite sprite)
        {
            foreach (var tag in _tags)
            {
                if (_tagIndex.ContainsKey(sprite))
                {
                    _tagIndex.Remove(sprite);
                }
                
                tag.Value.Remove(sprite);
            }
        }

        public void TrySetReindex(Sprite sprite)
        {
            if (!_tagIndex.ContainsKey(sprite)) return;
            
            foreach (var tag in _tagIndex[sprite])
            {
                if (_isChanged.ContainsKey(tag))
                {
                    _isChanged[tag] = true;
                }
            }
        }
    }
}