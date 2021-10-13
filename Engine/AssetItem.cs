namespace IsometricMagic.Engine
{
    class AssetItem
    {
        public string Path { get; }

        public string Type => System.IO.Path.GetExtension(Path)?.ToLower();

        public AssetItem(string path)
        {
            Path = path;
        }
    }
}