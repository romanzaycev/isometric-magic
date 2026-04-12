using IsometricMagic.Engine.Graphics;
using System.Diagnostics;

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
        public int SpritesVisited { get; private set; }
        public int SpritesSkipped { get; private set; }
        public int TextureBinds { get; private set; }
        public int TextureLoads { get; private set; }

        public int ActiveEntities { get; private set; }
        public int ComponentsUpdated { get; private set; }
        public int ComponentsLateUpdated { get; private set; }

        public float EventLoopMs { get; private set; }
        public float UpdateCpuMs { get; private set; }
        public float RenderCpuMs { get; private set; }
        public float SleepMs { get; private set; }
        public long GcAllocBytes { get; private set; }

        public int LastDrawCalls { get; private set; }
        public int LastSpritesDrawn { get; private set; }
        public int LastSpritesCulled { get; private set; }
        public int LastSpritesVisited { get; private set; }
        public int LastSpritesSkipped { get; private set; }
        public int LastTextureBinds { get; private set; }
        public int LastTextureLoads { get; private set; }

        public int LastActiveEntities { get; private set; }
        public int LastComponentsUpdated { get; private set; }
        public int LastComponentsLateUpdated { get; private set; }

        public float LastEventLoopMs { get; private set; }
        public float LastUpdateCpuMs { get; private set; }
        public float LastRenderCpuMs { get; private set; }
        public float LastSleepMs { get; private set; }
        public long LastGcAllocBytes { get; private set; }

        public float FrameMs { get; private set; }
        public float FrameMsAvg { get; private set; }
        public float FpsAvg { get; private set; }

        public int ViewportWidth { get; private set; }
        public int ViewportHeight { get; private set; }
        public GraphicsBackend Backend { get; private set; } = GraphicsBackend.OpenGL;
        public bool VSync { get; private set; }
        public string SceneName { get; private set; } = string.Empty;

#if DEBUG
        private long _gcAllocStartBytes;
#endif

        public static FrameStats GetInstance()
        {
            return Instance;
        }

        public void BeginFrame()
        {
            DrawCalls = 0;
            SpritesDrawn = 0;
            SpritesCulled = 0;
            SpritesVisited = 0;
            SpritesSkipped = 0;
            TextureBinds = 0;
            TextureLoads = 0;
            ActiveEntities = 0;
            ComponentsUpdated = 0;
            ComponentsLateUpdated = 0;
            EventLoopMs = 0f;
            UpdateCpuMs = 0f;
            RenderCpuMs = 0f;
            SleepMs = 0f;
            GcAllocBytes = 0;

#if DEBUG
            _gcAllocStartBytes = GC.GetAllocatedBytesForCurrentThread();
#endif
        }

        public void EndFrame(float deltaTime)
        {
            if (deltaTime > 0f)
            {
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

#if DEBUG
            var gcAllocEndBytes = GC.GetAllocatedBytesForCurrentThread();
            var gcAllocDelta = gcAllocEndBytes - _gcAllocStartBytes;
            GcAllocBytes = gcAllocDelta > 0 ? gcAllocDelta : 0;
#endif

            LastDrawCalls = DrawCalls;
            LastSpritesDrawn = SpritesDrawn;
            LastSpritesCulled = SpritesCulled;
            LastSpritesVisited = SpritesVisited;
            LastSpritesSkipped = SpritesSkipped;
            LastTextureBinds = TextureBinds;
            LastTextureLoads = TextureLoads;
            LastActiveEntities = ActiveEntities;
            LastComponentsUpdated = ComponentsUpdated;
            LastComponentsLateUpdated = ComponentsLateUpdated;
            LastEventLoopMs = EventLoopMs;
            LastUpdateCpuMs = UpdateCpuMs;
            LastRenderCpuMs = RenderCpuMs;
            LastSleepMs = SleepMs;
            LastGcAllocBytes = GcAllocBytes;
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

        [Conditional("DEBUG")]
        public void AddSpriteVisited()
        {
            SpritesVisited++;
        }

        [Conditional("DEBUG")]
        public void AddSpriteSkipped()
        {
            SpritesSkipped++;
        }

        [Conditional("DEBUG")]
        public void AddTextureBind(uint textureId)
        {
            if (textureId != 0u)
            {
                TextureBinds++;
            }
        }

        [Conditional("DEBUG")]
        public void AddTextureLoad()
        {
            TextureLoads++;
        }

        [Conditional("DEBUG")]
        public void SetActiveEntities(int count)
        {
            ActiveEntities = count;
        }

        [Conditional("DEBUG")]
        public void AddComponentUpdated()
        {
            ComponentsUpdated++;
        }

        [Conditional("DEBUG")]
        public void AddComponentLateUpdated()
        {
            ComponentsLateUpdated++;
        }

        [Conditional("DEBUG")]
        public void SetEventLoopMs(float value)
        {
            EventLoopMs = value;
        }

        [Conditional("DEBUG")]
        public void SetUpdateCpuMs(float value)
        {
            UpdateCpuMs = value;
        }

        [Conditional("DEBUG")]
        public void SetRenderCpuMs(float value)
        {
            RenderCpuMs = value;
        }

        [Conditional("DEBUG")]
        public void SetSleepMs(float value)
        {
            SleepMs = value;
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
