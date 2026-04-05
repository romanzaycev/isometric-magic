namespace IsometricMagic.Engine.App
{
    public interface IApplicationRuntimeService
    {
        void Initialize(AppConfig config);

        void Update();

        void Stop();
    }
}
