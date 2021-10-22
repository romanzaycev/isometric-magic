using System.Collections;

namespace IsometricMagic.Engine
{
    public class Scene
    {
        public const string MAIN = "main";
        public const string UI = "ui";

        protected static readonly SceneManager SceneManager = SceneManager.GetInstance();
        protected static readonly Camera Camera = Application.GetInstance().GetRenderer().GetCamera();
        protected static readonly Application Application = Application.GetInstance();
        
        private readonly string _name;
        public string Name => _name;

        private readonly SceneLayer _mainLayer;
        public SceneLayer MainLayer => _mainLayer;

        private readonly SceneLayer _uiLayer;
        public SceneLayer UiLayer => _uiLayer;

        protected bool _isAsyncInitializer = false;
        public bool IsAsyncInitializer => _isAsyncInitializer;

        public Scene(string name)
        {
            _name = name;
            _mainLayer = new SceneLayer(this, MAIN);
            _uiLayer = new SceneLayer(this, UI);
        }

        public Scene(string name, bool isAsyncInitializer)
        {
            _name = name;
            _isAsyncInitializer = isAsyncInitializer;
            _mainLayer = new SceneLayer(this, MAIN);
            _uiLayer = new SceneLayer(this, UI);
        }

        public void Load()
        {
            Initialize();
        }

        public IEnumerator LoadAsync()
        {
            return InitializeAsync();
        }

        public void Unload()
        {
            DeInitialize();

            for (int i = 0; i < _mainLayer.Sprites.Count; i++)
            {
                if (_mainLayer.Sprites[i].Texture != null)
                {
                    TextureHolder.GetInstance().Remove(_mainLayer.Sprites[i].Texture);
                }
                
                _mainLayer.Remove(_mainLayer.Sprites[i]);
            }

            for (int i = 0; i < _uiLayer.Sprites.Count; i++)
            {
                if (_uiLayer.Sprites[i].Texture != null) {
                    TextureHolder.GetInstance().Remove(_uiLayer.Sprites[i].Texture);
                }
                
                _uiLayer.Remove(_uiLayer.Sprites[i]);
            }
        }

        protected virtual void Initialize()
        {
        }
        
        protected virtual IEnumerator InitializeAsync()
        {
            yield break;
        }
        
        public virtual void Update()
        {
            
        }

        protected virtual void DeInitialize()
        {
            
        }
    }
}