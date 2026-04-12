using System;
using System.Numerics;

using IonMotion.Engine.Diagnostics;
using IonMotion.Engine.SceneGraph;

namespace IonMotion.Engine.Spatial
{
    [RuntimeEditorInspectable(EditableByDefault = false)]
    public class Transform2D
    {
        [RuntimeEditorEditable]
        public Vector2 LocalPosition
        {
            get => _localPosition;
            set
            {
                if (_localPosition == value)
                {
                    return;
                }

                _localPosition = value;
                _localVersion++;
            }
        }

        [RuntimeEditorEditable(Step = 0.1)]
        public double LocalRotation
        {
            get => _localRotation;
            set
            {
                if (_localRotation == value)
                {
                    return;
                }

                _localRotation = value;
                _localVersion++;
            }
        }

        private Vector2 _localPosition = Vector2.Zero;
        private double _localRotation;

        private Entity? _parent;
        public Entity? Parent => _parent;

        private Vector2 _cachedCanvasPosition;
        private double _cachedCanvasRotation;
        private ulong _localVersion;
        private ulong _cachedLocalVersion = ulong.MaxValue;
        private Transform2D? _cachedParent;
        private ulong _cachedParentWorldVersion = ulong.MaxValue;
        private ulong _worldVersion;

        public Vector2 CanvasPosition
        {
            get
            {
                EnsureCanvasUpToDate();
                return _cachedCanvasPosition;
            }
        }

        public double CanvasRotation
        {
            get
            {
                EnsureCanvasUpToDate();
                return _cachedCanvasRotation;
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
            else if (canvasPositionStays)
            {
                LocalPosition = oldCanvasPos;
                LocalRotation = oldCanvasRot;
            }
            else if (parent != null)
            {
                LocalPosition = oldCanvasPos;
                LocalRotation = oldCanvasRot;
            }
        }

        private void EnsureCanvasUpToDate()
        {
            var parentTransform = _parent?.Transform;
            if (parentTransform != null)
            {
                parentTransform.EnsureCanvasUpToDate();
            }

            var parentWorldVersion = parentTransform?._worldVersion ?? 0UL;
            if (_cachedLocalVersion == _localVersion
                && ReferenceEquals(_cachedParent, parentTransform)
                && _cachedParentWorldVersion == parentWorldVersion)
            {
                return;
            }

            if (parentTransform == null)
            {
                _cachedCanvasPosition = _localPosition;
                _cachedCanvasRotation = _localRotation;
            }
            else
            {
                var parentRotation = parentTransform._cachedCanvasRotation;
                if (parentRotation == 0.0)
                {
                    _cachedCanvasPosition = parentTransform._cachedCanvasPosition + _localPosition;
                }
                else
                {
                    var rotated = RotateVector(_localPosition, parentRotation);
                    _cachedCanvasPosition = parentTransform._cachedCanvasPosition + rotated;
                }

                _cachedCanvasRotation = parentTransform._cachedCanvasRotation + _localRotation;
            }

            _cachedLocalVersion = _localVersion;
            _cachedParent = parentTransform;
            _cachedParentWorldVersion = parentWorldVersion;
            _worldVersion++;
        }
    }
}
