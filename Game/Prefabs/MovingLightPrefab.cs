using System.Numerics;
using IonMotion.Game.Components.Movement;
using IonMotion.Game.Components.Vfx.Light;
using IonMotion.Game.Model;
using IonMotion.Game.Rendering;

namespace IonMotion.Game.Prefabs
{
    public readonly record struct MovingLightPrefabSpec(
        string EntityName,
        CanvasPosition OrbitCenterCanvas,
        float OrbitRadius,
        float OrbitSpeed,
        int LayerBase,
        int Bias = IsoSort.BiasVfx
    );

    public sealed class MovingLightPrefab
    {
        private readonly MovingLightPrefabSpec _spec;

        public MovingLightPrefab(MovingLightPrefabSpec spec)
        {
            _spec = spec;
        }

        public Entity Instantiate(Scene scene, Entity? parent = null)
        {
            var entity = scene.CreateEntity(_spec.EntityName, parent);
            
            entity.AddComponent(new OrbitMotionComponent
            {
                Center = _spec.OrbitCenterCanvas,
                Radius = _spec.OrbitRadius,
                Speed = _spec.OrbitSpeed
            });
            entity.AddComponent(new Light2DComponent
            {
                Intensity = 5f,
                Radius = 256f,
                Height = 2f,
                Falloff = 2f,
                InnerRadius = 64f,
                CenterAttenuation = 0.15f,
                Color = new Vector3(0.929f, 0.565f, 0.122f),
            });
            entity.AddComponent(new FireCircleComponent
            {
                TargetLayer = scene.MainLayer,
                LayerBase = _spec.LayerBase,
                Bias = _spec.Bias
            });

            return entity;
        }
    }
}
