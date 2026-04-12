using IonMotion.Engine.Assets;
using IonMotion.Engine.Graphics.OpenGL;
using IonMotion.Engine.Rendering;
using IonMotion.Engine.Scenes;

namespace IonMotion.Engine.Core.Graphics
{
    internal interface IGraphics
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
