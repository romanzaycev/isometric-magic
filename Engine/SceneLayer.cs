using System.Collections.Generic;

namespace IsometricMagic.Engine
{
    public class SceneLayer
    {
        private static readonly SpriteHolder SpriteHolder = SpriteHolder.GetInstance();
        
        private readonly string _name;
        public string Name => _name;

        private readonly List<Sprite> _sprites = new();
        public Sprite[] Sprites => _sprites.ToArray();

        public SceneLayer(string name)
        {
            _name = name;
        }

        public void Add(Sprite sprite)
        {
            _sprites.Add(sprite);
            SpriteHolder.Add(sprite, $"scene_{_name}");
        }

        public void Remove(Sprite sprite)
        {
            _sprites.Remove(sprite);
        }
    }
}