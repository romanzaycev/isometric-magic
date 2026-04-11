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
                var resourcesRoot = GetResourcesRoot();
                MountAutoPaksInternal(resourcesRoot);
                Vfs.MountDirectory(resourcesRoot, DiskPriority);
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

        public static string Path(string path)
        {
            return NormalizePath(path);
        }

        public static string Data(string relativePath)
        {
            return NormalizePath($"data/{relativePath}");
        }

        public static string Gen(string relativePath)
        {
            return NormalizePath($"_gen/{relativePath}");
        }

        public static string Engine(string relativePath)
        {
            return NormalizePath($"engine/{relativePath}");
        }

        public static string ResolveFromFile(string referrerFilePath, string referencedPath)
        {
            return ResourcePath.ResolveFromFile(referrerFilePath, referencedPath);
        }

        public static string ResolveFromDirectory(string baseDirectoryPath, string referencedPath)
        {
            return ResourcePath.ResolveFromDirectory(baseDirectoryPath, referencedPath);
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

        private static string GetResourcesRoot()
        {
            return System.IO.Path.Combine(AppContext.BaseDirectory, ResourcePath.EntryPoint);
        }

        private static void MountAutoPaksInternal(string resourcesRoot)
        {
            var paksDirectory = System.IO.Path.Combine(resourcesRoot, "paks");
            if (!Directory.Exists(paksDirectory))
            {
                return;
            }

            var pakFiles = Directory.GetFiles(paksDirectory, "*.pak", SearchOption.TopDirectoryOnly);
            Array.Sort(pakFiles, static (left, right) =>
                NaturalFileNameComparer.Instance.Compare(System.IO.Path.GetFileName(left), System.IO.Path.GetFileName(right)));

            foreach (var pakFile in pakFiles)
            {
                Vfs.MountPak(pakFile, PakPriority);
            }
        }
    }
}
