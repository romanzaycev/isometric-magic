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

        public Vector2 CanvasPosition
        {
            get
            {
                if (_parent == null || _parent.Transform == null)
                {
                    return LocalPosition;
                }

                var parentRot = _parent.Transform.CanvasRotation;
                if (parentRot == 0.0)
                {
                    return _parent.Transform.CanvasPosition + LocalPosition;
                }

                var rotated = RotateVector(LocalPosition, parentRot);
                return _parent.Transform.CanvasPosition + rotated;
            }
        }

        public double CanvasRotation
        {
            get
            {
                if (_parent == null || _parent.Transform == null)
                {
                    return LocalRotation;
                }

                return _parent.Transform.CanvasRotation + LocalRotation;
            }
        }

        public void SetCanvasPosition(Vector2 canvasPosition)
        {
            if (_parent == null || _parent.Transform == null)
            {
                LocalPosition = canvasPosition;
                return;
            }

            var parentTransform = _parent.Transform;
            var parentRot = parentTransform.CanvasRotation;
            var translated = canvasPosition - parentTransform.CanvasPosition;
            if (parentRot == 0.0)
            {
                LocalPosition = translated;
                return;
            }

            LocalPosition = RotateVector(translated, -parentRot);
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

        internal void SetParent(Entity? parent, bool canvasPositionStays)
        {
            if (_parent == parent) return;

            var oldCanvasPos = canvasPositionStays ? CanvasPosition : LocalPosition;
            var oldCanvasRot = canvasPositionStays ? CanvasRotation : LocalRotation;

            _parent = parent;

            if (canvasPositionStays && parent != null)
            {
                var parentTransform = parent.Transform;
                var parentRot = parentTransform?.CanvasRotation ?? 0.0;
                if (parentRot == 0.0)
                {
                    LocalPosition = parentTransform != null
                        ? oldCanvasPos - parentTransform.CanvasPosition
                        : oldCanvasPos;
                }
                else
                {
                    var invRot = -parentRot;
                    var translated = parentTransform != null
                        ? oldCanvasPos - parentTransform.CanvasPosition
                        : oldCanvasPos;
                    LocalPosition = RotateVector(translated, invRot);
                }
                LocalRotation = MathHelper.NormalizeNor(oldCanvasRot - parentRot);
            }
            else if (parent != null)
            {
                LocalPosition = oldCanvasPos;
                LocalRotation = oldCanvasRot;
            }
        }
    }
}
