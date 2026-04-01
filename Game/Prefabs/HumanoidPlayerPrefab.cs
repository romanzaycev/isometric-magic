using IsometricMagic.Engine;
using IsometricMagic.Game.Components.Actor;
using IsometricMagic.Game.Components.Camera;
using IsometricMagic.Game.Components.Character.Humanoid;
using IsometricMagic.Game.Components.Collision;
using IsometricMagic.Game.Components.Spatial;
using IsometricMagic.Game.Controllers.Character;
using IsometricMagic.Game.Model;

namespace IsometricMagic.Game.Prefabs
{
    public readonly record struct HumanoidPlayerPrefabSpec(
        string EntityName,
        IsoWorldPosition StartPosition,
        int WorldLayerBase,
        int AnimationSorting = 0,
        float ColliderRadius = 20f,
        bool ColliderDebug = false,
        int CameraMinX = -200,
        int CameraMinY = -200,
        int CameraCenterYOffset = -100
    );

    public sealed class HumanoidPlayerPrefab
    {
        private readonly HumanoidPlayerPrefabSpec _spec;

        public HumanoidPlayerPrefab(HumanoidPlayerPrefabSpec spec)
        {
            _spec = spec;
        }

        public Entity Instantiate(Scene scene, Entity? parent = null)
        {
            var entity = scene.CreateEntity(_spec.EntityName, parent);

            entity.AddComponent(new IsoWorldPositionComponent
            {
                Position = _spec.StartPosition
            });

            entity.AddComponent(new WorldColliderComponent
            {
                Radius = _spec.ColliderRadius,
                IsStatic = false,
                DebugDraw = _spec.ColliderDebug
            });

            entity.AddComponent(new MotorComponent());

            entity.AddComponent(new HumanoidAnimationComponent
            {
                TargetLayer = scene.MainLayer,
                Sorting = _spec.AnimationSorting
            });

            entity.AddComponent(new KeyboardOrGamepad());

            entity.AddComponent(new CameraFollowComponent
            {
                MinX = _spec.CameraMinX,
                MinY = _spec.CameraMinY,
                CenterYOffset = _spec.CameraCenterYOffset
            });

            entity.AddComponent(new HumanoidWorldPositionSyncComponent
            {
                LayerBase = _spec.WorldLayerBase
            });

            return entity;
        }
    }
}
