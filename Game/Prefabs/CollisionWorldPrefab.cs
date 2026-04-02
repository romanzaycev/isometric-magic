using IsometricMagic.Game.Components.Collision;

namespace IsometricMagic.Game.Prefabs
{
    public readonly record struct CollisionWorldPrefabSpec(
        string EntityName = "CollisionWorld",
        int CellSize = 128,
        float ContactSkin = 0.1f
    );

    public sealed class CollisionWorldPrefab
    {
        private readonly CollisionWorldPrefabSpec _spec;

        public CollisionWorldPrefab(CollisionWorldPrefabSpec spec)
        {
            _spec = spec with
            {
                EntityName = string.IsNullOrWhiteSpace(spec.EntityName) ? "CollisionWorld" : spec.EntityName,
                CellSize = spec.CellSize > 0 ? spec.CellSize : 128,
                ContactSkin = spec.ContactSkin > 0f ? spec.ContactSkin : 0.1f
            };
        }

        public Entity Instantiate(Scene scene, Entity? parent = null)
        {
            var entity = scene.CreateEntity(_spec.EntityName, parent);
            entity.AddComponent(new CollisionWorldComponent
            {
                CellSize = _spec.CellSize,
                ContactSkin = _spec.ContactSkin
            });
            return entity;
        }
    }
}
