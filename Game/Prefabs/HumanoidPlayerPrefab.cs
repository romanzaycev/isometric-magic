using System.Numerics;
using IsometricMagic.Game.Components.Actor;
using IsometricMagic.Game.Components.Camera;
using IsometricMagic.Game.Components.Character.Humanoid;
using IsometricMagic.Game.Components.Collision;
using IsometricMagic.Game.Components.Rendering;
using IsometricMagic.Game.Components.Spatial;
using IsometricMagic.Game.Controllers.Character;
using IsometricMagic.Game.Model;
using IsometricMagic.Game.Rendering;

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
        int CameraCenterYOffset = -100,
        int LayerStride = 0
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

            entity.AddComponent(new IsoWorldToCanvasPositionSyncComponent());

            entity.AddComponent(new WorldColliderComponent
            {
                Radius = _spec.ColliderRadius,
                IsStatic = false,
                DebugDraw = _spec.ColliderDebug,
                Offset = new Vector2(90, -90),
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

            entity.AddComponent(new HumanoidCanvasPositionSyncComponent
            {
                LayerBase = _spec.WorldLayerBase
            });
            
            var lightEf = new Entity(_spec.EntityName + "_LightEf")
            {
                Transform = {
                    LocalPosition = new Vector2(0, -70f)
                }
            };
            lightEf.SetParent(entity, false);
            lightEf.AddComponent(new SpriteRendererComponent
            {
                ImagePath = "./resources/data/textures/vfx/particles/circle_05.png",
                Width = 2048,
                Height = 1024,
                TextureWidth = 512,
                TextureHeight = 512,
                OriginPoint = OriginPoint.Centered,
                BlendMode = SpriteBlendMode.Overlay,
                Sorting = 0,
                TargetLayer = scene.MainLayer,
                Color = new Vector4(1f, 1f, 1f, 0.3f),
            });
            lightEf.AddComponent(new IsoDepthSortComponent
            {
                LayerBase = _spec.WorldLayerBase + _spec.LayerStride,
                Bias = IsoSort.BiasVfx,
            });

            entity.AddComponent(new IsoDepthSortComponent
            {
                LayerBase = _spec.WorldLayerBase,
                Bias = IsoSort.BiasActor,
            });

            return entity;
        }
    }
}
