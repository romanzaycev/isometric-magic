using System.Collections.Generic;

namespace IsometricMagic.Engine
{
    public class SceneLayer
    {
        private static readonly SpriteHolder SpriteHolder = SpriteHolder.GetInstance();

        private readonly Scene _scene;
        private readonly string _name;
        public string Name => _name;

        public IReadOnlyList<Sprite> Sprites => SpriteHolder.GetSprites($"scene_{_scene.Name}_{_name}");

        public SceneLayer(Scene scene, string name)
        {
            _scene = scene;
            _name = name;
        }

        public void Add(Sprite sprite)
        {
            SpriteHolder.Add(sprite, $"scene_{_scene.Name}_{_name}");
        }

        public void Remove(Sprite sprite)
        {
            SpriteHolder.Remove(sprite);
        }
    }
}