using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using IsometricMagic.Engine.App;
using IsometricMagic.Engine.Diagnostics;
using IsometricMagic.Engine.Graphics.Effects;
using IsometricMagic.Engine.Graphics.Lighting;
using IsometricMagic.Engine.Rendering;
using IsometricMagic.Engine.SceneGraph;
using IsometricMagic.Engine.Tweening;
using IsometricMagic.Engine.Core.Assets;

namespace IsometricMagic.Engine.Scenes
{
    public class Scene
    {
        public const string MAIN = "main";
        public const string UI = "ui";

        protected static SceneManager SceneManager => SceneManager.GetInstance();
        protected static Application Application => Application.GetInstance();
        private static readonly FrameStats FrameStats = FrameStats.GetInstance();

        private static readonly Camera FallbackCamera = new(0, 0);
        protected static Camera Camera
        {
            get
            {
                var renderer = Application.GetInstance().GetRenderer();
                if (renderer == null)
                {
                    return FallbackCamera;
                }

                return renderer.GetCamera();
            }
        }

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
        private readonly List<CameraInfluenceComponent> _activeCameraInfluenceComponents = new();
        private readonly List<Entity> _activeEntitiesPreorder = new();
        private bool _activeEntitiesDirty = true;

        private readonly List<Component> _searchComponentsPreorder = new();
        private readonly Dictionary<string, List<Entity>> _entitiesByName = new(StringComparer.Ordinal);
        private readonly Dictionary<string, List<Entity>> _entitiesByTag = new(StringComparer.Ordinal);
        private readonly Dictionary<Type, IReadOnlyList<Component>> _componentsByTypeCache = new();
        private bool _searchIndicesDirty = true;

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
            var dt = Time.DeltaTime;
            Tweens.Update(dt);
            Update();

            EnsureActiveEntitiesPreorder();
            FrameStats.SetActiveEntities(_activeEntitiesPreorder.Count);
            foreach (var entity in _activeEntitiesPreorder)
            {
                entity.CallSelfUpdate(dt);
            }

            foreach (var entity in _activeEntitiesPreorder)
            {
                entity.CallSelfLateUpdate(dt);
            }

            ProcessDestroyQueue();
        }

        internal void MarkActiveEntitiesDirty()
        {
            _activeEntitiesDirty = true;
        }

        private void EnsureActiveEntitiesPreorder()
        {
            if (!_activeEntitiesDirty)
            {
                return;
            }

            _activeEntitiesDirty = false;
            _activeEntitiesPreorder.Clear();
            CollectActiveEntitiesPreorder(Root, _activeEntitiesPreorder);
        }

        private static void CollectActiveEntitiesPreorder(Entity entity, List<Entity> buffer)
        {
            if (!entity.ActiveInHierarchy)
            {
                return;
            }

            buffer.Add(entity);
            foreach (var child in entity.Children)
            {
                CollectActiveEntitiesPreorder(child, buffer);
            }
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
            EnsureSearchIndices();
            if (!_entitiesByName.TryGetValue(name, out var matches) || matches.Count == 0)
            {
                return null;
            }

            return matches[0];
        }

        public Entity? FindActiveEntityByName(string name)
        {
            EnsureSearchIndices();
            if (!_entitiesByName.TryGetValue(name, out var matches) || matches.Count == 0)
            {
                return null;
            }

            foreach (var entity in matches)
            {
                if (entity.ActiveInHierarchy)
                {
                    return entity;
                }
            }

            return null;
        }

        public IEnumerable<Entity> FindEntitiesByTag(string tag)
        {
            EnsureSearchIndices();
            return _entitiesByTag.TryGetValue(tag, out var matches)
                ? matches
                : Enumerable.Empty<Entity>();
        }

        public IEnumerable<Entity> FindActiveEntitiesByTag(string tag)
        {
            EnsureSearchIndices();
            return _entitiesByTag.TryGetValue(tag, out var matches)
                ? matches.Where(static entity => entity.ActiveInHierarchy)
                : Enumerable.Empty<Entity>();
        }

        public T? FindComponent<T>() where T : Component
        {
            EnsureSearchIndices();
            foreach (var component in GetComponentsByTypeCached(typeof(T)))
            {
                if (component is T typed)
                {
                    return typed;
                }
            }

            return null;
        }

        public T? FindActiveComponent<T>() where T : Component
        {
            EnsureSearchIndices();
            foreach (var component in GetComponentsByTypeCached(typeof(T)))
            {
                if (component is T typed && typed.IsActiveAndEnabled)
                {
                    return typed;
                }
            }

            return null;
        }

        public IEnumerable<T> FindComponents<T>() where T : Component
        {
            EnsureSearchIndices();
            return GetComponentsByTypeCached(typeof(T)).OfType<T>();
        }

        public IEnumerable<T> FindActiveComponents<T>() where T : Component
        {
            EnsureSearchIndices();
            return GetComponentsByTypeCached(typeof(T))
                .OfType<T>()
                .Where(static component => component.IsActiveAndEnabled);
        }

        public void WarmupSearchIndices()
        {
            EnsureSearchIndices();
        }

        internal void CollectCameraInfluences(List<CameraInfluence> buffer)
        {
            buffer.Clear();
            for (var i = _activeCameraInfluenceComponents.Count - 1; i >= 0; i--)
            {
                var component = _activeCameraInfluenceComponents[i];
                if (component.Scene != this || !component.IsActiveAndEnabled)
                {
                    _activeCameraInfluenceComponents.RemoveAt(i);
                    continue;
                }

                component.CollectInfluence(buffer);
            }
        }

        internal void RegisterCameraInfluence(CameraInfluenceComponent component)
        {
            if (component.Scene != this || _activeCameraInfluenceComponents.Contains(component))
            {
                return;
            }

            _activeCameraInfluenceComponents.Add(component);
        }

        internal void UnregisterCameraInfluence(CameraInfluenceComponent component)
        {
            _activeCameraInfluenceComponents.Remove(component);
        }

        private IReadOnlyList<Component> GetComponentsByTypeCached(Type componentType)
        {
            if (_componentsByTypeCache.TryGetValue(componentType, out var cached))
            {
                return cached;
            }

            var matches = new List<Component>();
            foreach (var component in _searchComponentsPreorder)
            {
                if (componentType.IsInstanceOfType(component))
                {
                    matches.Add(component);
                }
            }

            cached = matches;
            _componentsByTypeCache[componentType] = cached;
            return cached;
        }

        private void EnsureSearchIndices()
        {
            if (!_searchIndicesDirty)
            {
                return;
            }

            _searchIndicesDirty = false;
            _searchComponentsPreorder.Clear();
            _entitiesByName.Clear();
            _entitiesByTag.Clear();
            _componentsByTypeCache.Clear();

            CollectSearchIndices(Root);
        }

        private void CollectSearchIndices(Entity parent)
        {
            foreach (var child in parent.Children)
            {
                AddEntityToStringIndex(_entitiesByName, child.Name, child);
                AddEntityToStringIndex(_entitiesByTag, child.Tag, child);

                foreach (var component in child.Components)
                {
                    _searchComponentsPreorder.Add(component);
                }

                CollectSearchIndices(child);
            }
        }

        private static void AddEntityToStringIndex(Dictionary<string, List<Entity>> index, string key, Entity entity)
        {
            if (!index.TryGetValue(key, out var list))
            {
                list = new List<Entity>();
                index[key] = list;
            }

            list.Add(entity);
        }

        internal void MarkSearchIndicesDirty()
        {
            _searchIndicesDirty = true;
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
            EnsureSearchIndices();
        }

        public IEnumerator LoadAsync()
        {
            return LoadAsyncAndWarmup();
        }

        private IEnumerator LoadAsyncAndWarmup()
        {
            var enumerator = InitializeAsync();
            while (enumerator.MoveNext())
            {
                yield return enumerator.Current;
            }

            if (enumerator is IDisposable disposable)
            {
                disposable.Dispose();
            }

            EnsureSearchIndices();
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
