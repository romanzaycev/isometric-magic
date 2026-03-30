using System.Collections;
using System.Collections.Generic;
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
        private static readonly Stack<IEnumerator> LoadingEnumeratorStack = new();
        private IGraphics _graphics = null!;
        private readonly CameraComposer _cameraComposer = new();
        private readonly List<CameraInfluence> _cameraInfluences = new();

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

            if (_config.TargetFps > 0)
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

            var scene = SceneManager.GetCurrent();
            scene.InternalUpdate();
            scene.CollectCameraInfluences(_cameraInfluences);
            _cameraComposer.Apply(_renderer.GetCamera(), _deltaTime, _cameraInfluences);
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
            if (LoadingEnumeratorStack.Count > 0)
            {
                AdvanceLoadingCoroutine();
            }
            
            var end = SDL_GetPerformanceCounter();
            var freq = SDL_GetPerformanceFrequency();

            if (_desiredDelta > 0 && freq > 0)
            {
                var delta = end - _startTick;
                var elapsedMs = (delta * 1000) / freq;

                if (_desiredDelta > elapsedMs)
                {
                    SDL_Delay((uint) Math.Floor((float) (_desiredDelta - elapsedMs)));
                }
            }
            else
            {
                var delta = end - _startTick;
                var elapsedMs = freq > 0 ? (double) delta * 1000.0 / freq : 0.0;

                if (elapsedMs < 1.0)
                {
                    SDL_Delay(1);
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
            LoadingEnumeratorStack.Clear();
            LoadingEnumeratorStack.Push(enumerator);
        }

        private static void AdvanceLoadingCoroutine()
        {
            if (LoadingEnumeratorStack.Count == 0)
            {
                return;
            }

            var current = LoadingEnumeratorStack.Peek();
            var hasNext = current.MoveNext();
            if (!hasNext)
            {
                var completed = LoadingEnumeratorStack.Pop();
                if (completed is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                if (LoadingEnumeratorStack.Count == 0)
                {
                    SceneManager.FinishAsync();
                }

                return;
            }

            if (current.Current is IEnumerator nested)
            {
                if (ContainsCoroutine(nested))
                {
                    throw new InvalidOperationException("Cyclic nested loading coroutine detected.");
                }

                LoadingEnumeratorStack.Push(nested);
            }
        }

        private static bool ContainsCoroutine(IEnumerator candidate)
        {
            foreach (var coroutine in LoadingEnumeratorStack)
            {
                if (ReferenceEquals(coroutine, candidate))
                {
                    return true;
                }
            }

            return false;
        }

        public IGraphics GetGraphics()
        {
            return _graphics;
        }
    }
}
