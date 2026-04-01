using System.Numerics;

using IsometricMagic.Engine.SceneGraph;

namespace IsometricMagic.Engine.Spatial
{
    public class Transform2D
    {
        public Vector2 LocalPosition = Vector2.Zero;
        public double LocalRotation = 0.0;

        private Entity? _parent;
        public Entity? Parent => _parent;

        public Vector2 WorldPosition
        {
            get
            {
                if (_parent == null || _parent.Transform == null)
                {
                    return LocalPosition;
                }

                var parentRot = _parent.Transform.WorldRotation;
                if (parentRot == 0.0)
                {
                    return _parent.Transform.WorldPosition + LocalPosition;
                }

                var rotated = RotateVector(LocalPosition, parentRot);
                return _parent.Transform.WorldPosition + rotated;
            }
        }

        public double WorldRotation
        {
            get
            {
                if (_parent == null || _parent.Transform == null)
                {
                    return LocalRotation;
                }

                return _parent.Transform.WorldRotation + LocalRotation;
            }
        }

        private static Vector2 RotateVector(Vector2 v, double angleNor)
        {
            if (angleNor == 0.0) return v;

            var angleDeg = 360.0 * angleNor;
            var angleRad = angleDeg * Math.PI / 180.0;
            var cos = MathF.Cos((float)angleRad);
            var sin = MathF.Sin((float)angleRad);

            return new Vector2(
                v.X * cos - v.Y * sin,
                v.X * sin + v.Y * cos
            );
        }

        internal void SetParent(Entity? parent, bool worldPositionStays)
        {
            if (_parent == parent) return;

            var oldWorldPos = worldPositionStays ? WorldPosition : LocalPosition;
            var oldWorldRot = worldPositionStays ? WorldRotation : LocalRotation;

            _parent = parent;

            if (worldPositionStays && parent != null)
            {
                var parentTransform = parent.Transform;
                var parentRot = parentTransform?.WorldRotation ?? 0.0;
                if (parentRot == 0.0)
                {
                    LocalPosition = parentTransform != null
                        ? oldWorldPos - parentTransform.WorldPosition
                        : oldWorldPos;
                }
                else
                {
                    var invRot = -parentRot;
                    var translated = parentTransform != null
                        ? oldWorldPos - parentTransform.WorldPosition
                        : oldWorldPos;
                    LocalPosition = RotateVector(translated, invRot);
                }
                LocalRotation = MathHelper.NormalizeNor(oldWorldRot - parentRot);
            }
            else if (parent != null)
            {
                LocalPosition = oldWorldPos;
                LocalRotation = oldWorldRot;
            }
        }
    }
}
