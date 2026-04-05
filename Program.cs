using IsometricMagic.Engine.App;
using IsometricMagic.Game.Scenes;
#if DEBUG
using IsometricMagic.RuntimeEditor;
#endif

namespace IsometricMagic
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            var host = ApplicationBuilder
                .CreateDefault()
                .UseConfig("config.ini")
                .ConfigureScenes(sceneManager =>
                {
                    sceneManager.SetLoadingScene(new Loading());
                    sceneManager.Add(new IsoTest());
                })
#if DEBUG
                .ConfigureRuntimeServices(services =>
                {
                    services.Add(new RuntimeEditorService());
                })
#endif
                .Build();

            host.Run();
        }
    }
}
