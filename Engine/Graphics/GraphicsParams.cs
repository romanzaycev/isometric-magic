namespace IsometricMagic.Engine.Graphics
{
    public class GraphicsParams
    {
        public int InitialWindowWidth { get; }
        public int InitialWindowHeight { get; }
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
    }
}