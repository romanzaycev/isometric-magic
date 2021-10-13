using System.Collections.Generic;

namespace IsometricMagic.Engine
{
    class SpriteHolder
    {
        private static readonly SpriteHolder Instance = new SpriteHolder();
        private readonly List<Sprite> _list = new List<Sprite>();

        public static SpriteHolder GetInstance()
        {
            return Instance;
        }

        public void PushSprite(Sprite sprite)
        {
            if (!_list.Contains(sprite))
            {
                _list.Add(sprite);
            }
        }

        public Sprite[] GetSprites()
        {
            return _list.ToArray();
        }

        public void Remove(Sprite sprite)
        {
            _list.Remove(sprite);
        }

        public void RemoveAll()
        {
            foreach (var sprite in _list)
            {
                sprite.Destroy();
            }
        }
    }
}