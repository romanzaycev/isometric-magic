using System;

using IsometricMagic.Engine.Diagnostics;
using IsometricMagic.Engine.Scenes;

namespace IsometricMagic.Engine.SceneGraph
{
    public abstract class Component
    {
        private Entity? _entity;
        public Entity? Entity => _entity;

        public Scene? Scene => _entity?.Scene;

        private bool _enabled = true;
        [RuntimeEditorEditable]
        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value) return;
                _enabled = value;

                if (_entity == null || !_entity.ActiveInHierarchy) return;

                if (_enabled)
                {
                    OnEnable();
                }
                else
                {
                    OnDisable();
                }
            }
        }

        public bool IsActiveAndEnabled => _enabled && (_entity?.ActiveInHierarchy ?? false);

        public virtual ComponentUpdateGroup UpdateGroup => ComponentUpdateGroup.Default;

        public virtual int UpdateOrder => 0;

        internal Entity? Owner
        {
            get => _entity;
            set
            {
                if (_entity == value) return;
                _entity = value;
            }
        }

        internal void CallAwake()
        {
            Awake();
        }

        internal void CallStart()
        {
            Start();
        }

        internal void CallUpdate(float dt)
        {
            if (!IsActiveAndEnabled) return;
            Update(dt);
        }

        internal void CallLateUpdate(float dt)
        {
            if (!IsActiveAndEnabled) return;
            LateUpdate(dt);
        }

        internal void CallOnEnable()
        {
            if (!IsActiveAndEnabled) return;
            OnEnable();
        }

        internal void CallOnDisable()
        {
            OnDisable();
        }

        internal void CallOnDestroy()
        {
            OnDestroy();
        }

        protected virtual void Awake() { }
        protected virtual void OnEnable() { }
        protected virtual void OnDisable() { }
        protected virtual void Start() { }
        protected virtual void Update(float dt) { }
        protected virtual void LateUpdate(float dt) { }
        protected virtual void OnDestroy() { }
    }
}
