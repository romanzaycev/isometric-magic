namespace IsometricMagic.Engine
{
    class Texture
    {
        private static readonly TextureHolder TextureHolder = TextureHolder.GetInstance();
        
        public int Width { get; }
        public int Height { get; }

        private AssetItem _image;
        private AssetItem Image => _image;
        public bool TextureTarget { get; }

        public Texture(int width, int height, bool textureTarget = false)
        {
            Width = width;
            Height = height;
            TextureTarget = textureTarget;
            TextureHolder.PushTexture(this);
        }

        public void LoadImage(AssetItem image)
        {
            _image = image;
            TextureHolder.LoadImage(this, image);
        }

        public void Destroy()
        {
            TextureHolder.Remove(this);
        }
    }
}