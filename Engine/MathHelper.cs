using System;

namespace IsometricMagic.Engine
{
    public static class MathHelper
    {
        public static double NorRotationToDegree(double angle)
        {
            return 360 * angle;
        }

        public static double NormalizeNor(double normalizedAngle)
        {
            bool isPositive = normalizedAngle > 0;
            
            normalizedAngle = Math.Abs(normalizedAngle);
            
            if (normalizedAngle <= 1)
            {
                return (isPositive) ? normalizedAngle : -normalizedAngle;
            }

            var nat = Math.Round(normalizedAngle, 0);
            
            normalizedAngle = normalizedAngle - nat;
            
            return (isPositive) ? normalizedAngle : -normalizedAngle;
        }
    }
}