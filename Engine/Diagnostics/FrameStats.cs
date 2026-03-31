using IsometricMagic.Engine.Graphics;

namespace IsometricMagic.Engine.Diagnostics
{
    public sealed class FrameStats
    {
        private static readonly FrameStats Instance = new();

        private double _sampleTime;
        private int _sampleFrames;

        public int DrawCalls { get; private set; }
        public int SpritesDrawn { get; private set; }
        public int SpritesCulled { get; private set; }

        public float FrameMs { get; private set; }
        public float FrameMsAvg { get; private set; }
        public float FpsAvg { get; private set; }

        public int ViewportWidth { get; private set; }
        public int ViewportHeight { get; private set; }
        public GraphicsBackend Backend { get; private set; } = GraphicsBackend.Sdl;
        public bool VSync { get; private set; }
        public string SceneName { get; private set; } = string.Empty;

        public static FrameStats GetInstance()
        {
            return Instance;
        }

        public void BeginFrame()
        {
            DrawCalls = 0;
            SpritesDrawn = 0;
            SpritesCulled = 0;
        }

        public void EndFrame(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            FrameMs = deltaTime * 1000f;
            _sampleTime += deltaTime;
            _sampleFrames++;

            if (_sampleTime >= 0.5)
            {
                var avgDelta = _sampleTime / _sampleFrames;
                FrameMsAvg = (float) (avgDelta * 1000.0);
                FpsAvg = avgDelta > 0.0 ? (float) (1.0 / avgDelta) : 0f;

                _sampleTime = 0.0;
                _sampleFrames = 0;
            }
        }

        public void AddDrawCall()
        {
            DrawCalls++;
        }

        public void AddSpriteDrawn()
        {
            SpritesDrawn++;
        }

        public void AddSpriteCulled()
        {
            SpritesCulled++;
        }

        public void SetViewport(int width, int height)
        {
            ViewportWidth = width;
            ViewportHeight = height;
        }

        public void SetBackend(GraphicsBackend backend)
        {
            Backend = backend;
        }

        public void SetVSync(bool value)
        {
            VSync = value;
        }

        public void SetSceneName(string sceneName)
        {
            SceneName = sceneName;
        }
    }
}
