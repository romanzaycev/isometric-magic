using System;
using System.Collections.Generic;
using System.Linq;

using IsometricMagic.Engine.Scenes;
using IsometricMagic.Engine.Spatial;

namespace IsometricMagic.Engine.SceneGraph
{
    public class Entity
    {
        private static readonly ComponentUpdateGroup[] UpdateGroupsInOrder =
        {
            ComponentUpdateGroup.Critical,
            ComponentUpdateGroup.Early,
            ComponentUpdateGroup.Default,
            ComponentUpdateGroup.Late
        };

        private static readonly SceneManager SceneManagerInstance = SceneManager.GetInstance();

        public string Name;
        public string Tag = string.Empty;

        public Transform2D Transform { get; } = new();

        private Entity? _parent;
        public Entity? Parent => _parent;

        private readonly List<Entity> _children = new();
        public IReadOnlyList<Entity> Children => _children;

        private bool _activeSelf = true;
        public bool ActiveSelf
        {
            get => _activeSelf;
            set
            {
                if (_activeSelf == value) return;
                _activeSelf = value;
                var newHierarchy = CalculateActiveInHierarchy();
                _activeInHierarchy = newHierarchy;
                PropagateActiveChange(newHierarchy);
            }
        }

        private bool _activeInHierarchy;
        public bool ActiveInHierarchy => _activeInHierarchy;

        private readonly List<Component> _components = new();
        private readonly List<Component> _orderedComponentsBuffer = new();
        public IReadOnlyList<Component> Components => _components;

        internal Scene? Scene { get; set; }

        internal void SetActiveInHierarchyInternal(bool value)
        {
            if (_activeInHierarchy == value) return;
            _activeInHierarchy = value;
            PropagateActiveChange(value);
        }

        private bool _destroyRequested;
        private bool _started;

        public Entity(string name)
        {
            Name = name;
        }

        public T AddComponent<T>() where T : Component, new()
        {
            var component = new T();
            AddComponent(component);
            return component;
        }

        public void AddComponent<T>(T component) where T : Component
        {
            if (component.Owner != null)
            {
                throw new InvalidOperationException("Component already belongs to an entity");
            }

            _components.Add(component);
            component.Owner = this;

            if (Scene != null)
            {
                component.CallAwake();
                if (_activeInHierarchy && component.Enabled)
                {
                    component.CallOnEnable();
                }
            }
        }

        public T? GetComponent<T>() where T : Component
        {
            foreach (var c in _components)
            {
                if (c is T result) return result;
            }
            return null;
        }

        public IEnumerable<T> GetComponents<T>() where T : Component
        {
            return _components.OfType<T>();
        }

        public T? GetComponentInChildren<T>(bool includeInactive = false) where T : Component
        {
            return FindInChildren<T>(this, includeInactive).FirstOrDefault();
        }

        public IEnumerable<T> GetComponentsInChildren<T>(bool includeInactive = false) where T : Component
        {
            return FindInChildren<T>(this, includeInactive);
        }

        private static IEnumerable<T> FindInChildren<T>(Entity entity, bool includeInactive) where T : Component
        {
            foreach (var child in entity._children)
            {
                if (!includeInactive && !child.ActiveInHierarchy) continue;

                foreach (var c in child._components)
                {
                    if (c is T result) yield return result;
                }

                foreach (var deeper in FindInChildren<T>(child, includeInactive))
                {
                    yield return deeper;
                }
            }
        }

        public void SetParent(Entity? newParent, bool canvasPositionStays = true)
        {
            if (_parent == newParent) return;

            if (_parent != null)
            {
                _parent._children.Remove(this);
            }

            var hadScene = Scene;

            _parent = newParent;

            if (_parent != null)
            {
                _parent._children.Add(this);
                if (_parent.Scene != null)
                {
                    SetSceneRecursive(_parent.Scene);
                }
            }
            else
            {
                SetSceneRecursive(null);
            }

            Transform.SetParent(newParent, canvasPositionStays);

            if (hadScene != Scene || _activeSelf)
            {
                var newHierarchy = CalculateActiveInHierarchy();
                if (newHierarchy != _activeInHierarchy)
                {
                    _activeInHierarchy = newHierarchy;
                    PropagateActiveChange(newHierarchy);
                }
            }
        }

        private void SetSceneRecursive(Scene? scene)
        {
            Scene = scene;
            foreach (var child in _children)
            {
                child.SetSceneRecursive(scene);
            }
        }

        private bool CalculateActiveInHierarchy()
        {
            if (!_activeSelf) return false;
            if (_parent == null) return Scene != null;
            return _parent._activeInHierarchy;
        }

        private void PropagateActiveChange(bool activeInHierarchy)
        {
            foreach (var component in _components)
            {
                if (component.Enabled)
                {
                    if (activeInHierarchy)
                    {
                        component.CallOnEnable();
                    }
                    else
                    {
                        component.CallOnDisable();
                    }
                }
            }

            foreach (var child in _children)
            {
                if (child._activeSelf)
                {
                    child._activeInHierarchy = activeInHierarchy;
                    child.PropagateActiveChange(activeInHierarchy);
                }
            }
        }

        internal void CallUpdate(float dt)
        {
            if (!_activeInHierarchy) return;

            if (!_started)
            {
                _started = true;
                foreach (var component in _components)
                {
                    component.CallStart();
                }
            }

            CallOrderedUpdate(dt, isLateUpdate: false);

            foreach (var child in _children)
            {
                child.CallUpdate(dt);
            }
        }

        internal void CallLateUpdate(float dt)
        {
            if (!_activeInHierarchy) return;

            CallOrderedUpdate(dt, isLateUpdate: true);

            foreach (var child in _children)
            {
                child.CallLateUpdate(dt);
            }
        }

        private void CallOrderedUpdate(float dt, bool isLateUpdate)
        {
            foreach (var group in UpdateGroupsInOrder)
            {
                _orderedComponentsBuffer.Clear();

                foreach (var component in _components)
                {
                    if (component.UpdateGroup == group)
                    {
                        _orderedComponentsBuffer.Add(component);
                    }
                }

                StableSortByUpdateOrder(_orderedComponentsBuffer);

                foreach (var component in _orderedComponentsBuffer)
                {
                    if (isLateUpdate)
                    {
                        component.CallLateUpdate(dt);
                    }
                    else
                    {
                        component.CallUpdate(dt);
                    }
                }
            }
        }

        private static void StableSortByUpdateOrder(List<Component> components)
        {
            for (var i = 1; i < components.Count; i++)
            {
                var key = components[i];
                var keyOrder = key.UpdateOrder;
                var j = i - 1;

                while (j >= 0 && components[j].UpdateOrder > keyOrder)
                {
                    components[j + 1] = components[j];
                    j--;
                }

                components[j + 1] = key;
            }
        }

        internal void RequestDestroy()
        {
            _destroyRequested = true;
            Scene?.AddToDestroyQueue(this);
        }

        internal void ProcessDestroy()
        {
            if (!_destroyRequested) return;
            _destroyRequested = false;

            foreach (var component in _components)
            {
                component.CallOnDestroy();
            }
            _components.Clear();

            var childrenCopy = _children.ToList();
            _children.Clear();
            foreach (var child in childrenCopy)
            {
                child.ProcessDestroy();
            }
        }

        public void Destroy()
        {
            RequestDestroy();
        }
    }
}
