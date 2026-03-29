using System.Collections.Generic;
using System.Drawing;
using System.Numerics;

namespace IsometricMagic.Engine
{
    public abstract class CameraInfluenceComponent : Component
    {
        public int Priority { get; set; } = 0;

        public abstract void CollectInfluence(List<CameraInfluence> buffer);

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
