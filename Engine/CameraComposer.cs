using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;

namespace IsometricMagic.Engine
{
    public class CameraComposer
    {
        public void Apply(Camera camera, float dt, List<CameraInfluence> influences)
        {
            if (influences.Count == 0)
            {
                return;
            }

            var rect = camera.Rect;
            var baseCenter = new Vector2(
                rect.X + rect.Width / 2f,
                rect.Y + rect.Height / 2f
            );

            var hasCenter = false;
            var centerPriority = int.MinValue;
            var center = baseCenter;
            var offset = Vector2.Zero;
            var shakeOffset = Vector2.Zero;

            var hasBounds = false;
            var boundsPriority = int.MinValue;
            var bounds = Rectangle.Empty;

            foreach (var influence in influences)
            {
                switch (influence.Kind)
                {
                    case CameraInfluenceKind.SetCenter:
                        if (influence.Priority >= centerPriority)
                        {
                            centerPriority = influence.Priority;
                            center = influence.Value;
                            hasCenter = true;
                        }
                        break;

                    case CameraInfluenceKind.AddOffset:
                        offset += influence.Value;
                        break;

                    case CameraInfluenceKind.Shake:
                        shakeOffset += influence.Value;
                        break;

                    case CameraInfluenceKind.ClampBounds:
                        if (influence.Bounds.HasValue && influence.Priority >= boundsPriority)
                        {
                            boundsPriority = influence.Priority;
                            bounds = influence.Bounds.Value;
                            hasBounds = true;
                        }
                        break;
                }
            }

            if (!hasCenter && offset == Vector2.Zero && shakeOffset == Vector2.Zero && !hasBounds)
            {
                return;
            }

            var finalCenter = hasCenter ? center : baseCenter;
            var finalOffset = offset + shakeOffset;

            rect.X = (int) MathF.Round(finalCenter.X - rect.Width / 2f + finalOffset.X);
            rect.Y = (int) MathF.Round(finalCenter.Y - rect.Height / 2f + finalOffset.Y);

            if (hasBounds)
            {
                rect = ClampRect(rect, bounds);
            }

            camera.Rect = rect;
        }

        private static Rectangle ClampRect(Rectangle rect, Rectangle bounds)
        {
            var minX = bounds.Left;
            var minY = bounds.Top;
            var maxX = bounds.Right - rect.Width;
            var maxY = bounds.Bottom - rect.Height;

            if (maxX < minX)
            {
                rect.X = minX;
            }
            else
            {
                rect.X = Math.Clamp(rect.X, minX, maxX);
            }

            if (maxY < minY)
            {
                rect.Y = minY;
            }
            else
            {
                rect.Y = Math.Clamp(rect.Y, minY, maxY);
            }

            return rect;
        }
    }
}
