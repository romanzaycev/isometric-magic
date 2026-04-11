using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

if (args.Length == 0)
{
    PrintUsage();
    return 1;
}

var command = args[0].Trim().ToLowerInvariant();
var options = ParseOptions(args.Skip(1).ToArray());

try
{
    return command switch
    {
        "pack" => RunPack(options),
        "unpack" => RunUnpack(options),
        "list" => RunList(options),
        _ => Fail($"Unknown command: {command}")
    };
}
catch (Exception ex)
{
    return Fail(ex.Message);
}

static int RunPack(Dictionary<string, string> options)
{
    var input = Require(options, "--input");
    var output = Require(options, "--out");
    var compression = options.TryGetValue("--compress", out var value)
        ? ParseCompression(value)
        : CompressionSelection.Auto;

    var inputRoot = Path.GetFullPath(input);
    if (!Directory.Exists(inputRoot))
    {
        return Fail($"Input directory does not exist: {inputRoot}");
    }

    var resourcesRoot = ResolveResourcesRoot(inputRoot);
    var files = DiscoverResourceFiles(resourcesRoot);
    if (files.Count == 0)
    {
        return Fail("No files found under resources/data or resources/_gen.");
    }

    Console.WriteLine($"Input: {resourcesRoot}");
    Console.WriteLine($"Files: {files.Count}");

    foreach (var entry in files)
    {
        entry.Method = SelectCompression(entry.Key, compression);
        entry.OriginalSize = new FileInfo(entry.SourcePath).Length;
        entry.StoredSize = ComputeStoredSize(entry.SourcePath, entry.Method);
    }

    var outputPath = Path.GetFullPath(output);
    var outputDirectory = Path.GetDirectoryName(outputPath)
        ?? throw new InvalidOperationException("Failed to resolve output directory.");
    Directory.CreateDirectory(outputDirectory);

    var xorKey = new byte[16];
    RandomNumberGenerator.Fill(xorKey);

    using var stream = new FileStream(outputPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None,
        bufferSize: 64 * 1024, FileOptions.SequentialScan);
    using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

    WriteHeader(writer, xorKey, indexOffset: 0, indexSize: 0);

    foreach (var entry in files)
    {
        entry.DataOffset = stream.Position;
        entry.StoredSize = WriteBlob(stream, entry.SourcePath, entry.Method, xorKey);
    }

    var indexOffset = stream.Position;
    var indexBytes = BuildIndex(files);
    WriteXored(stream, indexBytes, xorKey, indexOffset);
    var indexSize = indexBytes.LongLength;

    stream.Position = GetHeaderPatchOffset(xorKey.Length);
    writer.Write((ulong)indexOffset);
    writer.Write((ulong)indexSize);
    writer.Flush();

    Console.WriteLine($"Wrote: {outputPath}");
    Console.WriteLine($"Index: {indexOffset}..{indexOffset + indexSize - 1}");
    return 0;
}

static int RunUnpack(Dictionary<string, string> options)
{
    var input = Require(options, "--input");
    var output = Require(options, "--out");

    var inputPath = Path.GetFullPath(input);
    if (!File.Exists(inputPath))
    {
        return Fail($"Pak file does not exist: {inputPath}");
    }

    var outputDirectory = Path.GetFullPath(output);
    Directory.CreateDirectory(outputDirectory);

    using var pak = PakReader.Open(inputPath);
    foreach (var entry in pak.Entries)
    {
        var relative = entry.Key.StartsWith("resources/", StringComparison.Ordinal)
            ? entry.Key["resources/".Length..]
            : entry.Key;
        var destination = Path.Combine(outputDirectory, relative.Replace('/', Path.DirectorySeparatorChar));
        var destinationDirectory = Path.GetDirectoryName(destination);
        if (!string.IsNullOrEmpty(destinationDirectory))
        {
            Directory.CreateDirectory(destinationDirectory);
        }

        using var source = pak.OpenRead(entry.Key);
        using var destinationStream = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None);
        source.CopyTo(destinationStream);
    }

    Console.WriteLine($"Unpacked to: {outputDirectory}");
    return 0;
}

static int RunList(Dictionary<string, string> options)
{
    var input = Require(options, "--input");
    var inputPath = Path.GetFullPath(input);
    if (!File.Exists(inputPath))
    {
        return Fail($"Pak file does not exist: {inputPath}");
    }

    using var pak = PakReader.Open(inputPath);
    foreach (var entry in pak.Entries.OrderBy(e => e.Key, StringComparer.Ordinal))
    {
        Console.WriteLine($"{entry.Key}  ({entry.Value.OriginalSize} bytes)");
    }

    return 0;
}

static Dictionary<string, string> ParseOptions(string[] args)
{
    var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    for (var i = 0; i < args.Length; i++)
    {
        var key = args[i];
        if (!key.StartsWith("--", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Unexpected argument: {key}");
        }

        if (i + 1 >= args.Length)
        {
            throw new InvalidOperationException($"Missing value for argument: {key}");
        }

        map[key] = args[i + 1];
        i++;
    }

    return map;
}

static string Require(IReadOnlyDictionary<string, string> options, string key)
{
    if (!options.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
    {
        throw new InvalidOperationException($"Missing required argument: {key}");
    }

    return value;
}

static List<PackEntry> DiscoverResourceFiles(string resourcesRoot)
{
    var list = new List<PackEntry>();
    AddSubtree("data");
    AddSubtree("_gen");
    return list;

    void AddSubtree(string subtree)
    {
        var full = Path.Combine(resourcesRoot, subtree);
        if (!Directory.Exists(full))
        {
            return;
        }

        var files = Directory.GetFiles(full, "*", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            var rel = Path.GetRelativePath(resourcesRoot, file).Replace('\\', '/');
            var key = $"resources/{rel}".ToLowerInvariant();
            list.Add(new PackEntry(file, key));
        }
    }
}

static string ResolveResourcesRoot(string inputRoot)
{
    var normalized = Path.GetFullPath(inputRoot);
    if (Directory.Exists(Path.Combine(normalized, "data")) || Directory.Exists(Path.Combine(normalized, "_gen")))
    {
        return normalized;
    }

    var nested = Path.Combine(normalized, "resources");
    if (Directory.Exists(Path.Combine(nested, "data")) || Directory.Exists(Path.Combine(nested, "_gen")))
    {
        return nested;
    }

    return normalized;
}

static CompressionSelection ParseCompression(string value)
{
    return value.Trim().ToLowerInvariant() switch
    {
        "auto" => CompressionSelection.Auto,
        "none" => CompressionSelection.None,
        "fastest" => CompressionSelection.Fastest,
        _ => throw new InvalidOperationException("--compress must be one of: auto, none, fastest")
    };
}

static PakCompressionMethod SelectCompression(string key, CompressionSelection selection)
{
    if (selection == CompressionSelection.None)
    {
        return PakCompressionMethod.None;
    }

    if (selection == CompressionSelection.Fastest)
    {
        return PakCompressionMethod.DeflateFastest;
    }

    var extension = Path.GetExtension(key);
    return extension.ToLowerInvariant() switch
    {
        ".json" or ".ini" or ".txt" => PakCompressionMethod.DeflateFastest,
        _ => PakCompressionMethod.None
    };
}

static long ComputeStoredSize(string sourcePath, PakCompressionMethod method)
{
    if (method == PakCompressionMethod.None)
    {
        return new FileInfo(sourcePath).Length;
    }

    using var input = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
    using var counter = new CountingWriteStream();
    using (var deflate = new DeflateStream(counter, CompressionLevel.Fastest, leaveOpen: true))
    {
        input.CopyTo(deflate);
    }

    return counter.Length;
}

static long WriteBlob(FileStream output, string sourcePath, PakCompressionMethod method, byte[] xorKey)
{
    var start = output.Position;
    if (method == PakCompressionMethod.None)
    {
        var buffer = new byte[64 * 1024];
        using var input = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        while (true)
        {
            var read = input.Read(buffer, 0, buffer.Length);
            if (read <= 0)
            {
                break;
            }

            WriteXoredSpan(output, buffer.AsSpan(0, read), xorKey, output.Position);
        }

        return output.Position - start;
    }

    using var source = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
    using var xorStream = new XorWriteStream(output, xorKey);
    using (var deflate = new DeflateStream(xorStream, CompressionLevel.Fastest, leaveOpen: true))
    {
        source.CopyTo(deflate);
    }

    return xorStream.BytesWritten;
}

static byte[] BuildIndex(List<PackEntry> entries)
{
    using var memory = new MemoryStream();
    using var writer = new BinaryWriter(memory, Encoding.UTF8, leaveOpen: true);
    writer.Write((uint)entries.Count);

    foreach (var entry in entries)
    {
        var pathBytes = Encoding.UTF8.GetBytes(entry.Key);
        writer.Write((ushort)pathBytes.Length);
        writer.Write(pathBytes);
        writer.Write((ushort)entry.Method);
        writer.Write((ulong)entry.DataOffset);
        writer.Write((ulong)entry.StoredSize);
        writer.Write((ulong)entry.OriginalSize);
    }

    writer.Flush();
    return memory.ToArray();
}

static void WriteHeader(BinaryWriter writer, byte[] xorKey, ulong indexOffset, ulong indexSize)
{
    writer.Write(Encoding.ASCII.GetBytes("1MPAQ!"));
    writer.Write((ushort)1);
    writer.Write((ushort)xorKey.Length);
    writer.Write(xorKey);
    writer.Write(indexOffset);
    writer.Write(indexSize);
    writer.Flush();
}

static long GetHeaderPatchOffset(int keyLength)
{
    return 6 + 2 + 2 + keyLength;
}

static void WriteXored(FileStream output, byte[] data, byte[] xorKey, long absoluteOffset)
{
    WriteXoredSpan(output, data.AsSpan(), xorKey, absoluteOffset);
}

static void WriteXoredSpan(FileStream output, ReadOnlySpan<byte> data, byte[] xorKey, long absoluteOffset)
{
    var buffer = new byte[data.Length];
    data.CopyTo(buffer);

    for (var i = 0; i < buffer.Length; i++)
    {
        var keyIndex = (int)((absoluteOffset + i) % xorKey.Length);
        buffer[i] ^= xorKey[keyIndex];
    }

    output.Write(buffer);
}

static int Fail(string error)
{
    Console.Error.WriteLine(error);
    return 1;
}

static void PrintUsage()
{
    Console.WriteLine("IsometricMagic.PakTool");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  pack   --input <resources-dir> --out <pak-file> [--compress auto|none|fastest]");
    Console.WriteLine("  unpack --input <pak-file> --out <directory>");
    Console.WriteLine("  list   --input <pak-file>");
}

enum CompressionSelection
{
    Auto,
    None,
    Fastest,
}

enum PakCompressionMethod : ushort
{
    None = 0,
    DeflateFastest = 1,
}

sealed class PackEntry
{
    public PackEntry(string sourcePath, string key)
    {
        SourcePath = sourcePath;
        Key = key;
    }

    public string SourcePath { get; }
    public string Key { get; }
    public PakCompressionMethod Method { get; set; }
    public long DataOffset { get; set; }
    public long StoredSize { get; set; }
    public long OriginalSize { get; set; }
}

sealed class CountingWriteStream : Stream
{
    private long _length;
    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => _length;

    public override long Position
    {
        get => _length;
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        _length += count;
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        _length += buffer.Length;
    }
}

sealed class XorWriteStream : Stream
{
    private readonly FileStream _output;
    private readonly byte[] _xorKey;
    private readonly long _start;

    public XorWriteStream(FileStream output, byte[] xorKey)
    {
        _output = output;
        _xorKey = xorKey;
        _start = output.Position;
    }

    public long BytesWritten => _output.Position - _start;
    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => BytesWritten;

    public override long Position
    {
        get => BytesWritten;
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
        _output.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        Write(buffer.AsSpan(offset, count));
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        var copy = new byte[buffer.Length];
        buffer.CopyTo(copy);

        var absolute = _output.Position;
        for (var i = 0; i < copy.Length; i++)
        {
            var keyIndex = (int)((absolute + i) % _xorKey.Length);
            copy[i] ^= _xorKey[keyIndex];
        }

        _output.Write(copy);
    }
}

sealed class PakReader : IDisposable
{
    private readonly FileStream _stream;
    private readonly byte[] _xorKey;
    private readonly Dictionary<string, PakReaderEntry> _entries;

    private PakReader(FileStream stream, byte[] xorKey, Dictionary<string, PakReaderEntry> entries)
    {
        _stream = stream;
        _xorKey = xorKey;
        _entries = entries;
    }

    public IReadOnlyDictionary<string, PakReaderEntry> Entries => _entries;

    public static PakReader Open(string pakPath)
    {
        var stream = new FileStream(pakPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

        var magic = Encoding.ASCII.GetString(reader.ReadBytes(6));
        if (magic != "1MPAQ!")
        {
            throw new InvalidDataException("Invalid pak magic.");
        }

        var version = reader.ReadUInt16();
        if (version != 1)
        {
            throw new InvalidDataException($"Unsupported pak version: {version}");
        }

        var keyLength = reader.ReadUInt16();
        var key = reader.ReadBytes(keyLength);
        var indexOffset = (long)reader.ReadUInt64();
        var indexSize = (int)reader.ReadUInt64();

        stream.Position = indexOffset;
        var indexBytes = new byte[indexSize];
        ReadExactly(stream, indexBytes);
        ApplyXor(indexBytes, indexOffset, key);

        var entries = ParseIndex(indexBytes);
        return new PakReader(stream, key, entries);
    }

    public Stream OpenRead(string key)
    {
        if (!_entries.TryGetValue(key, out var entry))
        {
            throw new FileNotFoundException($"Entry '{key}' not found in pak.", key);
        }

        var segment = new PakSegmentStream(_stream, entry.Offset, entry.StoredSize, _xorKey);
        if (entry.Method == PakCompressionMethod.None)
        {
            return segment;
        }

        return new DeflateStream(segment, CompressionMode.Decompress);
    }

    public void Dispose()
    {
        _stream.Dispose();
    }

    private static Dictionary<string, PakReaderEntry> ParseIndex(byte[] indexBytes)
    {
        using var memory = new MemoryStream(indexBytes);
        using var reader = new BinaryReader(memory, Encoding.UTF8, leaveOpen: false);

        var count = reader.ReadUInt32();
        var entries = new Dictionary<string, PakReaderEntry>((int)count, StringComparer.Ordinal);
        for (var i = 0; i < count; i++)
        {
            var pathLength = reader.ReadUInt16();
            var path = Encoding.UTF8.GetString(reader.ReadBytes(pathLength));
            var method = (PakCompressionMethod)reader.ReadUInt16();
            var offset = (long)reader.ReadUInt64();
            var stored = (long)reader.ReadUInt64();
            var original = (long)reader.ReadUInt64();

            entries[path] = new PakReaderEntry(path, offset, stored, original, method);
        }

        return entries;
    }

    private static void ReadExactly(Stream stream, byte[] buffer)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = stream.Read(buffer, offset, buffer.Length - offset);
            if (read == 0)
            {
                throw new EndOfStreamException("Unexpected end of pak file.");
            }

            offset += read;
        }
    }

    private static void ApplyXor(byte[] bytes, long absoluteOffset, byte[] xorKey)
    {
        for (var i = 0; i < bytes.Length; i++)
        {
            var keyIndex = (int)((absoluteOffset + i) % xorKey.Length);
            bytes[i] ^= xorKey[keyIndex];
        }
    }

    sealed class PakSegmentStream : Stream
    {
        private readonly Stream _source;
        private readonly long _offset;
        private readonly long _length;
        private readonly byte[] _xorKey;
        private long _position;

        public PakSegmentStream(Stream source, long offset, long length, byte[] xorKey)
        {
            _source = source;
            _offset = offset;
            _length = length;
            _xorKey = xorKey;
            _source.Position = _offset;
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => _length;

        public override long Position
        {
            get => _position;
            set
            {
                _position = Math.Clamp(value, 0, _length);
                _source.Position = _offset + _position;
            }
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_position >= _length)
            {
                return 0;
            }

            var remaining = (int)Math.Min(count, _length - _position);
            var read = _source.Read(buffer, offset, remaining);
            if (read <= 0)
            {
                return 0;
            }

            var absolute = _offset + _position;
            for (var i = 0; i < read; i++)
            {
                var keyIndex = (int)((absolute + i) % _xorKey.Length);
                buffer[offset + i] ^= _xorKey[keyIndex];
            }

            _position += read;
            return read;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            Position = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => _position + offset,
                SeekOrigin.End => _length + offset,
                _ => _position
            };

            return _position;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}

sealed record PakReaderEntry(
    string Key,
    long Offset,
    long StoredSize,
    long OriginalSize,
    PakCompressionMethod Method);
