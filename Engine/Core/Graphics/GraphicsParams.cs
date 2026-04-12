namespace IonMotion.Engine.Core.Graphics
{
    internal class GraphicsParams
    {
        public int InitialWindowWidth { get; }
        public int InitialWindowHeight { get; }
        public string WindowTitle { get; private set; } = "IonMotion";
        public bool IsFullscreen { get; private set; } = false;
        public bool VSync { get; private set; } = false;

        public GraphicsParams(int initialWindowWidth, int initialWindowHeight)
        {
            InitialWindowWidth = initialWindowWidth;
            InitialWindowHeight = initialWindowHeight;
        }
        
        public GraphicsParams SetFullscreen(bool isFullscreen)
        {
            IsFullscreen = isFullscreen;
            
            return this;
        }

        public GraphicsParams SetVSync(bool isVSyncEnabled)
        {
            VSync = isVSyncEnabled;

            return this;
        }

        public GraphicsParams SetWindowTitle(string windowTitle)
        {
            WindowTitle = string.IsNullOrWhiteSpace(windowTitle) ? WindowTitle : windowTitle;

            return this;
        }
    }
}
