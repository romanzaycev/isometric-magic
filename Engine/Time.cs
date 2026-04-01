namespace IsometricMagic.Engine
{
    public static class Time
    {
        public static float DeltaTime { get; private set; }

        internal static void SetDeltaTime(float deltaTime)
        {
            DeltaTime = deltaTime;
        }
    }
}
