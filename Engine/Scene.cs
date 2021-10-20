namespace IsometricMagic.Engine
{
    public class Scene
    {
        public static readonly string MAIN = "main";
        public static readonly string UI = "ui";

        protected static readonly SceneManager SceneManager = SceneManager.GetInstance();
        protected static readonly Camera Camera = Application.GetInstance().GetRenderer().GetCamera();
        
        private readonly string _name;
        public string Name => _name;

        private readonly SceneLayer _mainLayer = new("main");
        public SceneLayer MainLayer => _mainLayer;

        private readonly SceneLayer _uiLayer = new("ui");
        public SceneLayer UiLayer => _uiLayer;

        public Scene(string name)
        {
            _name = name;
        }

        public void Load()
        {
            Initialize();
        }

        public void Unload()
        {
            foreach (var sprite in _mainLayer.Sprites)
            {
                _mainLayer.Remove(sprite);

                if (sprite.Texture != null)
                {
                    TextureHolder.GetInstance().Remove(sprite.Texture);
                }
            }
            
            foreach (var sprite in _uiLayer.Sprites)
            {
                _uiLayer.Remove(sprite);
                
                if (sprite.Texture != null) {
                    TextureHolder.GetInstance().Remove(sprite.Texture);
                }
            }
        }

        protected virtual void Initialize()
        {
            
        }
        
        public virtual void Update()
        {
            
        }
    }
}