using IsometricMagic.Engine.Core.Assets;

namespace IsometricMagic.Engine.Assets
{
    public class Texture
    {
        private static readonly TextureHolder TextureHolder = TextureHolder.GetInstance();
        
        public int Width { get; }
        public int Height { get; }

        private string? _image;

        public string? ImagePath => _image;
        public bool TextureTarget { get; }

        public Texture(int width, int height, bool textureTarget = false)
        {
            Width = width;
            Height = height;
            TextureTarget = textureTarget;

            if (textureTarget)
            {
                TextureHolder.PushTexture(this);
            }
        }

        public void LoadImage(string imagePath)
        {
            _image = imagePath;
            TextureHolder.PushTexture(this);
            TextureHolder.LoadImage(this, imagePath);
        }

        public void Destroy()
        {
            TextureHolder.Remove(this);
        }

        public static Texture AcquireShared(string imagePath, int width, int height)
        {
            return TextureHolder.GetInstance().AcquireSharedTexture(imagePath, width, height);
        }
    }
}
