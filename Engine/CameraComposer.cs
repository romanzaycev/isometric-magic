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

            var targetX = finalCenter.X - rect.Width / 2f + finalOffset.X;
            var targetY = finalCenter.Y - rect.Height / 2f + finalOffset.Y;

            if (hasBounds)
            {
                ClampPosition(ref targetX, ref targetY, rect.Width, rect.Height, bounds);
            }

            rect.X = (int) MathF.Floor(targetX);
            rect.Y = (int) MathF.Floor(targetY);
            camera.SubpixelOffset = new Vector2(targetX - rect.X, targetY - rect.Y);

            camera.Rect = rect;
        }

        private static void ClampPosition(ref float x, ref float y, int width, int height, Rectangle bounds)
        {
            var minX = (float) bounds.Left;
            var minY = (float) bounds.Top;
            var maxX = (float) (bounds.Right - width);
            var maxY = (float) (bounds.Bottom - height);

            if (maxX < minX)
            {
                x = minX;
            }
            else
            {
                x = Math.Clamp(x, minX, maxX);
            }

            if (maxY < minY)
            {
                y = minY;
            }
            else
            {
                y = Math.Clamp(y, minY, maxY);
            }
        }
    }
}
