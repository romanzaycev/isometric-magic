using System.Collections;
using System.Collections.Generic;
using System.Linq;
using IsometricMagic.Engine.Graphics.Effects;
using IsometricMagic.Engine.Graphics.Lighting;
using IsometricMagic.Engine.Tweening;

namespace IsometricMagic.Engine
{
    public class Scene
    {
        public const string MAIN = "main";
        public const string UI = "ui";

        protected static readonly SceneManager SceneManager = SceneManager.GetInstance();
        protected static readonly Camera Camera = Application.GetInstance().GetRenderer().GetCamera();
        protected static readonly Application Application = Application.GetInstance();

        private readonly string _name;
        public string Name => _name;

        private readonly SceneLayer _mainLayer;
        public SceneLayer MainLayer => _mainLayer;

        private readonly SceneLayer _uiLayer;
        public SceneLayer UiLayer => _uiLayer;

        protected bool _isAsyncInitializer = false;
        public bool IsAsyncInitializer => _isAsyncInitializer;

        private readonly PostProcessStack _postProcess = new();
        public PostProcessStack PostProcess => _postProcess;

        private readonly SceneLighting _lighting = new();
        public SceneLighting Lighting => _lighting;

        public TweenManager Tweens { get; } = new();

        public Entity Root { get; }

        private readonly List<Entity> _entityDestroyQueue = new();

        public Scene(string name)
        {
            _name = name;
            _mainLayer = new SceneLayer(this, MAIN);
            _uiLayer = new SceneLayer(this, UI);
            Root = new Entity("SceneRoot");
            Root.Scene = this;
            Root.SetActiveInHierarchyInternal(true);
        }

        public Scene(string name, bool isAsyncInitializer)
        {
            _name = name;
            _isAsyncInitializer = isAsyncInitializer;
            _mainLayer = new SceneLayer(this, MAIN);
            _uiLayer = new SceneLayer(this, UI);
            Root = new Entity("SceneRoot");
            Root.Scene = this;
            Root.SetActiveInHierarchyInternal(true);
        }

        public Entity CreateEntity(string name, Entity? parent = null)
        {
            var entity = new Entity(name);
            entity.SetParent(parent ?? Root, true);
            return entity;
        }

        public void InternalUpdate()
        {
            var dt = Application.DeltaTime;
            Tweens.Update(dt);
            Update();
            Root.CallUpdate(dt);
            Root.CallLateUpdate(dt);

            ProcessDestroyQueue();
        }

        private void ProcessDestroyQueue()
        {
            foreach (var entity in _entityDestroyQueue)
            {
                entity.ProcessDestroy();
            }
            _entityDestroyQueue.Clear();
        }

        public Entity? FindEntityByName(string name)
        {
            return FindEntityByNameRecursive(Root, name);
        }

        private static Entity? FindEntityByNameRecursive(Entity parent, string name)
        {
            foreach (var child in parent.Children)
            {
                if (child.Name == name) return child;
                var found = FindEntityByNameRecursive(child, name);
                if (found != null) return found;
            }
            return null;
        }

        public IEnumerable<Entity> FindEntitiesByTag(string tag)
        {
            return FindEntitiesByTagRecursive(Root, tag);
        }

        private static IEnumerable<Entity> FindEntitiesByTagRecursive(Entity parent, string tag)
        {
            foreach (var child in parent.Children)
            {
                if (child.Tag == tag) yield return child;
                foreach (var deeper in FindEntitiesByTagRecursive(child, tag))
                {
                    yield return deeper;
                }
            }
        }

        public T? FindComponent<T>() where T : Component
        {
            return FindComponentRecursive<T>(Root).FirstOrDefault();
        }

        public IEnumerable<T> FindComponents<T>() where T : Component
        {
            return FindComponentRecursive<T>(Root);
        }

        internal void CollectCameraInfluences(List<CameraInfluence> buffer)
        {
            buffer.Clear();
            CollectCameraInfluencesRecursive(Root, buffer);
        }

        private static IEnumerable<T> FindComponentRecursive<T>(Entity parent) where T : Component
        {
            foreach (var child in parent.Children)
            {
                foreach (var c in child.Components)
                {
                    if (c is T result) yield return result;
                }

                foreach (var deeper in FindComponentRecursive<T>(child))
                {
                    yield return deeper;
                }
            }
        }

        private static void CollectCameraInfluencesRecursive(Entity entity, List<CameraInfluence> buffer)
        {
            foreach (var component in entity.Components)
            {
                if (component is CameraInfluenceComponent influenceComponent && influenceComponent.IsActiveAndEnabled)
                {
                    influenceComponent.CollectInfluence(buffer);
                }
            }

            foreach (var child in entity.Children)
            {
                CollectCameraInfluencesRecursive(child, buffer);
            }
        }

        internal void AddToDestroyQueue(Entity entity)
        {
            if (!_entityDestroyQueue.Contains(entity))
            {
                _entityDestroyQueue.Add(entity);
            }
        }

        public void Load()
        {
            Initialize();
        }

        public IEnumerator LoadAsync()
        {
            return InitializeAsync();
        }

        public void Unload()
        {
            Tweens.Clear();
            DeInitialize();

            Root.RequestDestroy();
            ProcessDestroyQueue();

            var mainSprites = _mainLayer.Sprites.ToList();
            foreach (var sprite in mainSprites)
            {
                if (sprite.Texture != null)
                {
                    TextureHolder.GetInstance().Remove(sprite.Texture);
                }
                _mainLayer.Remove(sprite);
            }

            var uiSprites = _uiLayer.Sprites.ToList();
            foreach (var sprite in uiSprites)
            {
                if (sprite.Texture != null)
                {
                    TextureHolder.GetInstance().Remove(sprite.Texture);
                }
                _uiLayer.Remove(sprite);
            }
        }

        protected virtual void Initialize()
        {
        }

        protected virtual IEnumerator InitializeAsync()
        {
            yield break;
        }

        public virtual void Update()
        {
        }

        protected virtual void DeInitialize()
        {
        }
    }
}
