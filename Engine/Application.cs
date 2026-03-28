using System.Collections;
using IsometricMagic.Engine.Graphics;
using static SDL2.SDL;

namespace IsometricMagic.Engine
{
    public class Application
    {
        private static readonly Application Instance = new();
        private static readonly SceneManager SceneManager = SceneManager.GetInstance();
        private AppConfig _config = null!;
        private bool _isInitialized;
        private Renderer _renderer = null!;
        private bool _repaintWindowNeeded;
        private ulong _desiredDelta;
        private ulong _startTick;

        private static ulong _dtLast;
        private static float _deltaTime;
        public static float DeltaTime => _deltaTime;

        private int _viewportWidth;
        private int _viewportHeight;
        private static IEnumerator? _loadingEnumerator;
        private IGraphics _graphics = null!;

        public int ViewportWidth => _viewportWidth;
        public int ViewportHeight => _viewportHeight;
        
        public static Application GetInstance()
        {
            return Instance;
        }

        public void Init(AppConfig config, IGraphics graphics)
        {
            if (_isInitialized)
            {
                return;
            }

            _config = config;
            _graphics = graphics;
            
            var graphicsParams = new GraphicsParams(_config.WindowWidth, _config.WindowHeight)
                .SetFullscreen(_config.IsFullscreen)
                .SetVSync(_config.VSync);
            _graphics.Initialize(graphicsParams);
            
            _renderer = new Renderer(graphics);

            _repaintWindowNeeded = true;
            _isInitialized = true;

            if (_config.TargetFps > 0 && !_config.VSync)
            {
                _desiredDelta = 1000 / (ulong) _config.TargetFps;
            }

            _dtLast = 0;

            RepaintWindow();
        }

        public void Stop()
        {
            if (!_isInitialized) return;

            SceneManager.GetCurrent().Unload();
            TextureHolder.GetInstance().DestroyAll();
            _graphics.Stop();
        }

        public void Update()
        {
            var now = SDL_GetTicks();

            if (now > _dtLast)
            {
                _deltaTime =  (float)(now - _dtLast) / 1000;
                _dtLast = now;
            }

            SceneManager.GetCurrent().Update();
            _renderer.GetCamera().Controller?.UpdateCamera(_renderer.GetCamera());
            _renderer.DrawAll();
            RepaintWindow();
        }

        public void HandleWindowEvent(SDL_Event sdlEvent)
        {
            if (sdlEvent.window.windowEvent == SDL_WindowEventID.SDL_WINDOWEVENT_RESIZED)
            {
                _repaintWindowNeeded = true;
            }
        }

        public Renderer GetRenderer()
        {
            return _renderer;
        }

        public void StartTick()
        {
            _startTick = SDL_GetPerformanceCounter();
        }

        public void EndTick()
        {
            if (_loadingEnumerator != null)
            {
                var isLoaded = _loadingEnumerator.MoveNext();
                
                if (!isLoaded)
                {
                    SceneManager.FinishAsync();
                    _loadingEnumerator = null;
                }
            }
            
            var end = SDL_GetPerformanceCounter();
            var freq = SDL_GetPerformanceFrequency();

            if (_desiredDelta > 0 && freq > 0)
            {
                var delta = end - _startTick;
                var elapsedMs = delta / freq * 1000;

                if (_desiredDelta > elapsedMs)
                {
                    SDL_Delay((uint) Math.Floor((float) (_desiredDelta - elapsedMs)));
                }
            }
        }

        public AppConfig GetConfig()
        {
            return _config;
        }

        private void RepaintWindow()
        {
            if (!_repaintWindowNeeded)
            {
                return;
            }

            _repaintWindowNeeded = false;
            _graphics.RepaintWindow(out var w, out var h);
            _viewportWidth = w;
            _viewportHeight = h;
            _renderer.HandleWindowResized(w, h);
        }

        public void LoadingCoroutinePush(IEnumerator enumerator)
        {
            _loadingEnumerator = enumerator;
        }

        public IGraphics GetGraphics()
        {
            return _graphics;
        }
    }
}
