using System.Collections.Generic;

using IonMotion.Engine.Core.Rendering;
using IonMotion.Engine.Rendering;

namespace IonMotion.Engine.Scenes
{
    public class SceneLayer
    {
        private static readonly SpriteHolder SpriteHolder = SpriteHolder.GetInstance();

        private readonly Scene _scene;
        private readonly string _name;
        private readonly string _spriteGroupKey;
        public string Name => _name;

        public IReadOnlyList<Sprite> Sprites => SpriteHolder.GetSprites(_spriteGroupKey);

        public SceneLayer(Scene scene, string name)
        {
            _scene = scene;
            _name = name;
            _spriteGroupKey = $"scene_{_scene.Name}_{_name}";
        }

        public void Add(Sprite sprite)
        {
            SpriteHolder.Add(sprite, _spriteGroupKey);
        }

        public void Remove(Sprite sprite)
        {
            SpriteHolder.Remove(sprite);
        }
    }
}
