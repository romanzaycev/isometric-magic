using System.Numerics;
using IsometricMagic.Engine;
using IsometricMagic.Game.Components.Spatial;
using IsometricMagic.Game.Components.Vfx.Light;
using IsometricMagic.Game.Rendering;

namespace IsometricMagic.Game.Prefabs
{
    public readonly record struct MovingLightPrefabSpec(
        string EntityName,
        Vector2 OrbitCenterCanvas,
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
            entity.AddComponent(new WorldPositionComponent());
            entity.AddComponent(new OrbitLightComponent
            {
                Center = _spec.OrbitCenterCanvas,
                Radius = _spec.OrbitRadius,
                Speed = _spec.OrbitSpeed
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
