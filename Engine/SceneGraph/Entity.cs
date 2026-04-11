using System;
using System.Collections.Generic;
using System.Linq;

using IsometricMagic.Engine.Diagnostics;
using IsometricMagic.Engine.Scenes;
using IsometricMagic.Engine.Spatial;

namespace IsometricMagic.Engine.SceneGraph
{
    [RuntimeEditorInspectable(EditableByDefault = false)]
    public class Entity
    {
        [RuntimeEditorEditable]
        public string Name;

        [RuntimeEditorEditable]
        public string Tag = string.Empty;

        public Transform2D Transform { get; } = new();

        private Entity? _parent;
        public Entity? Parent => _parent;

        private readonly List<Entity> _children = new();
        public IReadOnlyList<Entity> Children => _children;

        private bool _activeSelf = true;
        [RuntimeEditorEditable]
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
                Scene?.MarkActiveEntitiesDirty();
            }
        }

        private bool _activeInHierarchy;
        public bool ActiveInHierarchy => _activeInHierarchy;

        private readonly List<Component> _components = new();
        private readonly List<Component> _activeCriticalComponents = new();
        private readonly List<Component> _activeEarlyComponents = new();
        private readonly List<Component> _activeDefaultComponents = new();
        private readonly List<Component> _activeLateComponents = new();
        private readonly List<Component> _executionBuffer = new();
        public IReadOnlyList<Component> Components => _components;

        internal Scene? Scene { get; set; }

        internal void SetActiveInHierarchyInternal(bool value)
        {
            if (_activeInHierarchy == value) return;
            _activeInHierarchy = value;
            PropagateActiveChange(value);
            Scene?.MarkActiveEntitiesDirty();
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

                RebuildActiveComponentLists();
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

        public T? GetComponentInParent<T>(bool includeInactive = false) where T : Component
        {
            for (Entity? current = this; current != null; current = current._parent)
            {
                if (!includeInactive && !current.ActiveInHierarchy)
                {
                    continue;
                }

                foreach (var c in current._components)
                {
                    if (c is T result)
                    {
                        return result;
                    }
                }
            }

            return null;
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

            var previousScene = Scene;

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

            previousScene?.MarkActiveEntitiesDirty();
            Scene?.MarkActiveEntitiesDirty();
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

            RebuildActiveComponentLists();

            foreach (var child in _children)
            {
                if (child._activeSelf)
                {
                    child._activeInHierarchy = activeInHierarchy;
                    child.PropagateActiveChange(activeInHierarchy);
                }
            }
        }

        internal void NotifyComponentEnabledStateChanged()
        {
            if (!_activeInHierarchy)
            {
                return;
            }

            RebuildActiveComponentLists();
        }

        internal void CallSelfUpdate(float dt)
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

            CallGroupUpdate(_activeCriticalComponents, dt, isLateUpdate: false);
            CallGroupUpdate(_activeEarlyComponents, dt, isLateUpdate: false);
            CallGroupUpdate(_activeDefaultComponents, dt, isLateUpdate: false);
            CallGroupUpdate(_activeLateComponents, dt, isLateUpdate: false);
        }

        internal void CallSelfLateUpdate(float dt)
        {
            if (!_activeInHierarchy) return;

            CallGroupUpdate(_activeCriticalComponents, dt, isLateUpdate: true);
            CallGroupUpdate(_activeEarlyComponents, dt, isLateUpdate: true);
            CallGroupUpdate(_activeDefaultComponents, dt, isLateUpdate: true);
            CallGroupUpdate(_activeLateComponents, dt, isLateUpdate: true);
        }

        private void CallGroupUpdate(List<Component> components, float dt, bool isLateUpdate)
        {
            _executionBuffer.Clear();
            _executionBuffer.AddRange(components);

            for (var i = 0; i < _executionBuffer.Count; i++)
            {
                var component = _executionBuffer[i];
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

        private void RebuildActiveComponentLists()
        {
            _activeCriticalComponents.Clear();
            _activeEarlyComponents.Clear();
            _activeDefaultComponents.Clear();
            _activeLateComponents.Clear();

            if (!_activeInHierarchy)
            {
                return;
            }

            foreach (var component in _components)
            {
                if (!component.Enabled)
                {
                    continue;
                }

                GetGroupList(component.UpdateGroup).Add(component);
            }

            StableSortByUpdateOrder(_activeCriticalComponents);
            StableSortByUpdateOrder(_activeEarlyComponents);
            StableSortByUpdateOrder(_activeDefaultComponents);
            StableSortByUpdateOrder(_activeLateComponents);
        }

        private List<Component> GetGroupList(ComponentUpdateGroup group)
        {
            return group switch
            {
                ComponentUpdateGroup.Critical => _activeCriticalComponents,
                ComponentUpdateGroup.Early => _activeEarlyComponents,
                ComponentUpdateGroup.Default => _activeDefaultComponents,
                ComponentUpdateGroup.Late => _activeLateComponents,
                _ => _activeDefaultComponents
            };
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
            DestroyRecursive();
        }

        private void DestroyRecursive()
        {
            _destroyRequested = false;
            var previousScene = Scene;

            foreach (var component in _components)
            {
                component.CallOnDestroy();
            }
            _components.Clear();
            RebuildActiveComponentLists();

            var childrenCopy = _children.ToList();
            foreach (var child in childrenCopy)
            {
                child.DestroyRecursive();
            }
            _children.Clear();

            if (_parent != null)
            {
                _parent._children.Remove(this);
                _parent = null;
            }

            Scene = null;
            _activeInHierarchy = false;

            previousScene?.MarkActiveEntitiesDirty();
        }

        public void Destroy()
        {
            RequestDestroy();
        }
    }
}
