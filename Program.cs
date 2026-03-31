using IsometricMagic.Engine;
using IsometricMagic.Game.Scenes;

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
                .Build();

            host.Run();
        }
    }
}
