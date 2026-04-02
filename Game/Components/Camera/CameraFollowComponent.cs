using System.Numerics;

namespace IsometricMagic.Game.Components.Camera
{
    public class CameraFollowComponent : CameraInfluenceComponent
    {
        public int MinX { get; set; } = -200;
        public int MinY { get; set; } = -200;
        public int CenterYOffset { get; set; } = -100;

        public override ComponentUpdateGroup UpdateGroup => ComponentUpdateGroup.Late;

        public override int UpdateOrder => 400;

        public CameraFollowComponent()
        {
            Priority = 100;
        }

        public override void CollectInfluence(List<CameraInfluence> buffer)
        {
            if (Entity == null) return;

            var targetCenter = Entity.Transform.CanvasPosition + new Vector2(0f, CenterYOffset);

            var camera = Application.GetInstance().GetRenderer().GetCamera();
            var rect = camera.Rect;
            var nextX = targetCenter.X - rect.Width / 2f;
            var nextY = targetCenter.Y - rect.Height / 2f;

            if (nextX < MinX)
            {
                targetCenter.X = MinX + rect.Width / 2f;
            }

            if (nextY < MinY)
            {
                targetCenter.Y = MinY + rect.Height / 2f;
            }

            AddSetCenter(buffer, targetCenter, Priority);
        }
    }
}
