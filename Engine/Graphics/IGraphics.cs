namespace IsometricMagic.Engine.Graphics
{
    public interface IGraphics
    {
        public void Initialize(GraphicsParams graphicsParams);
        
        public void Stop();

        public void RepaintWindow(out int width, out int height);
        
        public void Draw(Scene scene, Camera camera);

        public NativeTexture CreateTexture(PixelFormat format, TextureAccess access, int width, int height);
        
        public void DestroyTexture(NativeTexture nativeTexture);
        
        public void LoadImageToTexture(out NativeTexture nativeTexture, string imagePath);
    }
}