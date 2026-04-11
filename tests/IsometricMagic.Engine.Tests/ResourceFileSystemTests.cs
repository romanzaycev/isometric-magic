using System.IO.Compression;
using System.Text;
using IsometricMagic.Engine.Assets;
using IsometricMagic.Engine.Core.Assets;
using Xunit;

namespace IsometricMagic.Engine.Tests;

public sealed class ResourceFileSystemTests
{
    [Fact]
    public void NormalizePath_MapsToResourcesEntryPointAndLowerCase()
    {
        var normalized = ResourceFileSystem.NormalizePath("./resources/DATA/Maps/Map1.JSON");
        Assert.Equal("resources/data/maps/map1.json", normalized);
    }

    [Fact]
    public void VirtualFileSystem_LastPakWins_DiskOverridesPak()
    {
        var root = CreateTempDir();
        try
        {
            var resourcesDir = Path.Combine(root, "resources");
            var diskDataDir = Path.Combine(resourcesDir, "data");
            Directory.CreateDirectory(diskDataDir);

            var pak1Path = Path.Combine(root, "a.pak");
            var pak2Path = Path.Combine(root, "b.pak");

            WritePak(pak1Path, new[]
            {
                new PakTestEntry("resources/data/test.txt", "pak-a", Compress: false),
            });
            WritePak(pak2Path, new[]
            {
                new PakTestEntry("resources/data/test.txt", "pak-b", Compress: false),
            });

            File.WriteAllText(Path.Combine(diskDataDir, "test.txt"), "disk");

            using var vfs = new VirtualFileSystem();
            vfs.MountPak(pak1Path, priority: 100);
            vfs.MountPak(pak2Path, priority: 100);
            Assert.Equal("pak-b", ReadUtf8(vfs.OpenRead("resources/data/test.txt")));

            vfs.MountDirectory(resourcesDir, priority: 1000);
            Assert.Equal("disk", ReadUtf8(vfs.OpenRead("resources/data/test.txt")));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void VirtualFileSystem_ReadsDeflatedEntry()
    {
        var root = CreateTempDir();
        try
        {
            var pakPath = Path.Combine(root, "compressed.pak");
            WritePak(pakPath, new[]
            {
                new PakTestEntry("resources/data/maps/map.json", "{\"name\":\"demo\"}", Compress: true),
            });

            using var vfs = new VirtualFileSystem();
            vfs.MountPak(pakPath, priority: 100);
            Assert.Equal("{\"name\":\"demo\"}", ReadUtf8(vfs.OpenRead("resources/data/maps/map.json")));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string ReadUtf8(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    private static string CreateTempDir()
    {
        var path = Path.Combine(Path.GetTempPath(), $"isometric_magic_vfs_{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void WritePak(string path, IReadOnlyList<PakTestEntry> entries)
    {
        var key = new byte[] { 0x12, 0x34, 0x56, 0x78 };
        var packed = entries.Select(e => new MutableEntry(e.Path.ToLowerInvariant(), Encoding.UTF8.GetBytes(e.Text), e.Compress)).ToList();

        using var stream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

        writer.Write(Encoding.ASCII.GetBytes("1MPAQ!"));
        writer.Write((ushort)1);
        writer.Write((ushort)key.Length);
        writer.Write(key);
        writer.Write((ulong)0);
        writer.Write((ulong)0);

        foreach (var entry in packed)
        {
            entry.Offset = stream.Position;

            byte[] payload;
            if (entry.Compress)
            {
                using var compressed = new MemoryStream();
                using (var deflate = new DeflateStream(compressed, CompressionLevel.Fastest, leaveOpen: true))
                {
                    deflate.Write(entry.OriginalBytes);
                }

                payload = compressed.ToArray();
                entry.Method = 1;
            }
            else
            {
                payload = entry.OriginalBytes;
                entry.Method = 0;
            }

            entry.StoredSize = payload.Length;
            WriteXored(stream, payload, key, stream.Position);
        }

        var indexOffset = stream.Position;
        var indexBytes = BuildIndex(packed);
        WriteXored(stream, indexBytes, key, indexOffset);
        var indexSize = indexBytes.Length;

        stream.Position = 6 + 2 + 2 + key.Length;
        writer.Write((ulong)indexOffset);
        writer.Write((ulong)indexSize);
    }

    private static byte[] BuildIndex(List<MutableEntry> entries)
    {
        using var memory = new MemoryStream();
        using var writer = new BinaryWriter(memory, Encoding.UTF8, leaveOpen: true);

        writer.Write((uint)entries.Count);
        foreach (var entry in entries)
        {
            var pathBytes = Encoding.UTF8.GetBytes(entry.Path);
            writer.Write((ushort)pathBytes.Length);
            writer.Write(pathBytes);
            writer.Write((ushort)entry.Method);
            writer.Write((ulong)entry.Offset);
            writer.Write((ulong)entry.StoredSize);
            writer.Write((ulong)entry.OriginalBytes.Length);
        }

        writer.Flush();
        return memory.ToArray();
    }

    private static void WriteXored(FileStream stream, byte[] data, byte[] key, long absoluteOffset)
    {
        var copy = new byte[data.Length];
        Buffer.BlockCopy(data, 0, copy, 0, data.Length);
        for (var i = 0; i < copy.Length; i++)
        {
            copy[i] ^= key[(absoluteOffset + i) % key.Length];
        }

        stream.Write(copy, 0, copy.Length);
    }

    private sealed class MutableEntry
    {
        public MutableEntry(string path, byte[] originalBytes, bool compress)
        {
            Path = path;
            OriginalBytes = originalBytes;
            Compress = compress;
        }

        public string Path { get; }
        public byte[] OriginalBytes { get; }
        public bool Compress { get; }
        public ushort Method { get; set; }
        public long Offset { get; set; }
        public long StoredSize { get; set; }
    }

    private sealed record PakTestEntry(string Path, string Text, bool Compress);
}
