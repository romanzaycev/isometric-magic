using IonMotion.Game.Scenes;
#if DEBUG
using IonMotion.RuntimeEditor;
#endif

namespace IonMotion
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            var host = ApplicationBuilder
                .CreateDefault()
                .UseAppName("IonMotion")
                .UseConfig("config.ini")
                .ConfigureScenes(sceneManager =>
                {
                    sceneManager.SetLoadingScene(new Loading());
                    //sceneManager.Add(new Benchmark());
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
