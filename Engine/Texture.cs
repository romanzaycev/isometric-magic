namespace IsometricMagic.Engine
{
    public class Texture
    {
        private static readonly TextureHolder TextureHolder = TextureHolder.GetInstance();
        
        public int Width { get; }
        public int Height { get; }

        private string _image;

        private string Image => _image;
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
    }
}