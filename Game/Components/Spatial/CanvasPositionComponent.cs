using IsometricMagic.Game.Model;

namespace IsometricMagic.Game.Components.Spatial
{
    public class CanvasPositionComponent : Component
    {
        public CanvasPosition Position = new(0f, 0f);

        public override ComponentUpdateGroup UpdateGroup => ComponentUpdateGroup.Early;

        public override int UpdateOrder => 200;

        public float X
        {
            get => Position.X;
            set => Position = Position with { X = value };
        }

        public float Y
        {
            get => Position.Y;
            set => Position = Position with { Y = value };
        }

        protected override void Update(float dt)
        {
            if (Entity == null)
            {
                return;
            }

            Entity.Transform.SetCanvasPosition(Position.ToVector2());
        }
    }
}
