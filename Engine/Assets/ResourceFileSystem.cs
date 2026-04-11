using System.Text;
using IsometricMagic.Engine.Core.Assets;

namespace IsometricMagic.Engine.Assets
{
    public static class ResourceFileSystem
    {
        private const int PakPriority = 100;
        private const int DiskPriority = 1000;

        private static readonly VirtualFileSystem Vfs = new();
        private static readonly object Sync = new();

        public static void InitializeDefault()
        {
            lock (Sync)
            {
                Vfs.Reset();
                MountAutoPaksInternal();
                Vfs.MountDirectory($"./{ResourcePath.EntryPoint}", DiskPriority);
            }
        }

        public static void MountPak(string pakPath)
        {
            lock (Sync)
            {
                Vfs.MountPak(pakPath, PakPriority);
            }
        }

        public static bool Exists(string path)
        {
            return Vfs.Exists(path);
        }

        public static Stream OpenRead(string path)
        {
            return Vfs.OpenRead(path);
        }

        public static byte[] ReadAllBytes(string path)
        {
            using var stream = OpenRead(path);
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            return memory.ToArray();
        }

        public static string ReadAllText(string path, Encoding? encoding = null)
        {
            using var stream = OpenRead(path);
            using var reader = new StreamReader(stream, encoding ?? Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            return reader.ReadToEnd();
        }

        public static string NormalizePath(string path)
        {
            return ResourcePath.NormalizeResourcePath(path);
        }

        public static void Shutdown()
        {
            lock (Sync)
            {
                Vfs.Reset();
            }
        }

        internal static void ResetForTests()
        {
            Shutdown();
        }

        private static void MountAutoPaksInternal()
        {
            var paksDirectory = Path.GetFullPath($"./{ResourcePath.EntryPoint}/paks");
            if (!Directory.Exists(paksDirectory))
            {
                return;
            }

            var pakFiles = Directory.GetFiles(paksDirectory, "*.pak", SearchOption.TopDirectoryOnly);
            Array.Sort(pakFiles, static (left, right) =>
                NaturalFileNameComparer.Instance.Compare(Path.GetFileName(left), Path.GetFileName(right)));

            foreach (var pakFile in pakFiles)
            {
                Vfs.MountPak(pakFile, PakPriority);
            }
        }
    }
}
