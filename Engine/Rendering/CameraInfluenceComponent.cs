using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using IonMotion.Engine.SceneGraph;
using IonMotion.Engine.Scenes;

namespace IonMotion.Engine.Rendering
{
    public abstract class CameraInfluenceComponent : Component
    {
        private Scene? _registeredScene;

        public int Priority { get; set; } = 0;

        public abstract void CollectInfluence(List<CameraInfluence> buffer);

        protected override void OnEnable()
        {
            var scene = Scene;
            if (scene == null || ReferenceEquals(scene, _registeredScene))
            {
                return;
            }

            _registeredScene?.UnregisterCameraInfluence(this);
            scene.RegisterCameraInfluence(this);
            _registeredScene = scene;
        }

        protected override void OnDisable()
        {
            UnregisterFromScene();
        }

        protected override void OnDestroy()
        {
            UnregisterFromScene();
        }

        private void UnregisterFromScene()
        {
            var scene = _registeredScene ?? Scene;
            scene?.UnregisterCameraInfluence(this);
            _registeredScene = null;
        }

        protected void AddSetCenter(List<CameraInfluence> buffer, Vector2 center, int? priority = null)
        {
            buffer.Add(CameraInfluence.SetCenter(center, priority ?? Priority));
        }

        protected void AddOffset(List<CameraInfluence> buffer, Vector2 offset, int? priority = null)
        {
            buffer.Add(CameraInfluence.AddOffset(offset, priority ?? Priority));
        }

        protected void AddShake(List<CameraInfluence> buffer, Vector2 offset, int? priority = null)
        {
            buffer.Add(CameraInfluence.Shake(offset, priority ?? Priority));
        }

        protected void AddClampBounds(List<CameraInfluence> buffer, Rectangle bounds, int? priority = null)
        {
            buffer.Add(CameraInfluence.ClampBounds(bounds, priority ?? Priority));
        }
    }
}
