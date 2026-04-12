using System.IO.Compression;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace IonMotion.Engine.Core.Assets
{
    /*
     * PAK v1 file layout (little-endian):
     *
     * [Header, plain]
     *  - Magic        : 6 bytes  ASCII "1MPAQ!"
     *  - Version      : u16      currently 1
     *  - KeyLength    : u16      XOR key length, must be > 0
     *  - XorKey       : byte[]   KeyLength bytes, embedded in the archive
     *  - IndexOffset  : u64      absolute file offset of encrypted index blob
     *  - IndexSize    : u64      size of encrypted index blob in bytes
     *
     * [Payload, XOR-obfuscated]
     *  - File blobs   : concatenated file payload blocks at recorded offsets
     *  - Index blob   : file table used for O(1) lookup by normalized path
     *
     * XOR rule:
     *  - Header is never XOR'ed.
     *  - Every byte in file blobs and index blob is XOR'ed with:
     *      key[(absoluteFileOffset + localByteIndex) % keyLength]
     *    where absoluteFileOffset is the byte position in the .pak file.
     *  - This offset-based rule allows random-access reads/decode without
     *    decrypting from the beginning of the archive.
     *
     * Index blob format (after XOR decode):
     *  - EntryCount    : u32
     *  - Repeated EntryCount times:
     *      PathLength  : u16
     *      PathBytes   : UTF-8 bytes (resource key)
     *      Flags       : u16 (compression method enum)
     *      DataOffset  : u64 (absolute file offset of payload block)
     *      StoredSize  : u64 (payload size in archive, after compression)
     *      OriginalSize: u64 (size after decompression)
     *
     * Path semantics:
     *  - Stored keys are normalized resource paths under the "resources"
     *    entry point and compared case-insensitively via lower-case canonical
     *    form (see ResourcePath.Normalize).
     *
     * Compression flags:
     *  - 0 = None
     *  - 1 = DeflateFastest
     */
    internal sealed class PakArchive : IDisposable
    {
        private const string Magic = "1MPAQ!";

        private readonly FileStream _stream;
        private readonly byte[] _xorKey;
        private readonly Dictionary<string, PakEntry> _entries;

        private PakArchive(FileStream stream, byte[] xorKey, Dictionary<string, PakEntry> entries)
        {
            _stream = stream;
            _xorKey = xorKey;
            _entries = entries;
        }

        public IReadOnlyCollection<string> Paths => _entries.Keys;

        public static PakArchive Open(string pakPath)
        {
            var fullPath = Path.GetFullPath(pakPath);
            var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read,
                bufferSize: 64 * 1024, FileOptions.RandomAccess);

            try
            {
                using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

                var magic = Encoding.ASCII.GetString(reader.ReadBytes(Magic.Length));
                if (!string.Equals(magic, Magic, StringComparison.Ordinal))
                {
                    throw new InvalidDataException($"Invalid pak magic in '{pakPath}'.");
                }

                var version = reader.ReadUInt16();
                if (version != 1)
                {
                    throw new InvalidDataException($"Unsupported pak version: {version}.");
                }

                var keyLength = reader.ReadUInt16();
                if (keyLength == 0)
                {
                    throw new InvalidDataException("Pak XOR key length cannot be zero.");
                }

                var xorKey = reader.ReadBytes(keyLength);
                var indexOffset = (long)reader.ReadUInt64();
                var indexSize = (int)reader.ReadUInt64();

                if (indexOffset < 0 || indexSize < 0 || indexOffset + indexSize > stream.Length)
                {
                    throw new InvalidDataException("Pak index points outside the archive.");
                }

                var indexBytes = new byte[indexSize];
                stream.Position = indexOffset;
                ReadExactly(stream, indexBytes);
                ApplyXor(indexBytes, indexOffset, xorKey);

                var entries = ParseIndex(indexBytes);
                return new PakArchive(stream, xorKey, entries);
            }
            catch
            {
                stream.Dispose();
                throw;
            }
        }

        public bool Contains(string path)
        {
            var key = ResourcePath.NormalizeResourcePath(path);
            return _entries.ContainsKey(key);
        }

        public Stream OpenRead(string path)
        {
            var key = ResourcePath.NormalizeResourcePath(path);
            if (!_entries.TryGetValue(key, out var entry))
            {
                throw new FileNotFoundException($"Resource '{path}' not found in pak archive.", path);
            }

            var segment = new PakSegmentReadStream(_stream.SafeFileHandle, entry.Offset, entry.StoredSize, _xorKey);

            return entry.Compression switch
            {
                PakCompressionMethod.None => segment,
                PakCompressionMethod.DeflateFastest => new DeflateStream(segment, CompressionMode.Decompress),
                _ => throw new InvalidDataException($"Unsupported compression method '{entry.Compression}'.")
            };
        }

        public void Dispose()
        {
            _stream.Dispose();
        }

        private static Dictionary<string, PakEntry> ParseIndex(byte[] indexBytes)
        {
            using var memory = new MemoryStream(indexBytes, writable: false);
            using var reader = new BinaryReader(memory, Encoding.UTF8, leaveOpen: false);
            var entryCount = reader.ReadUInt32();

            var entries = new Dictionary<string, PakEntry>((int)entryCount, StringComparer.Ordinal);

            for (var i = 0; i < entryCount; i++)
            {
                var pathLength = reader.ReadUInt16();
                var pathBytes = reader.ReadBytes(pathLength);
                var path = ResourcePath.NormalizeResourcePath(Encoding.UTF8.GetString(pathBytes));

                var flags = reader.ReadUInt16();
                var offset = (long)reader.ReadUInt64();
                var storedSize = (long)reader.ReadUInt64();
                var originalSize = (long)reader.ReadUInt64();

                entries[path] = new PakEntry(path, offset, storedSize, originalSize, (PakCompressionMethod)flags);
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
                    throw new EndOfStreamException("Unexpected EOF while reading pak archive.");
                }

                offset += read;
            }
        }

        private static void ApplyXor(byte[] buffer, long absoluteOffset, byte[] key)
        {
            for (var i = 0; i < buffer.Length; i++)
            {
                var keyIndex = (int)((absoluteOffset + i) % key.Length);
                buffer[i] ^= key[keyIndex];
            }
        }

        private sealed class PakSegmentReadStream : Stream
        {
            private readonly SafeFileHandle _handle;
            private readonly long _startOffset;
            private readonly long _length;
            private readonly byte[] _xorKey;
            private long _position;

            public PakSegmentReadStream(SafeFileHandle handle, long startOffset, long length, byte[] xorKey)
            {
                _handle = handle;
                _startOffset = startOffset;
                _length = length;
                _xorKey = xorKey;
            }

            public override bool CanRead => true;
            public override bool CanSeek => true;
            public override bool CanWrite => false;
            public override long Length => _length;

            public override long Position
            {
                get => _position;
                set => _position = Math.Clamp(value, 0, _length);
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return Read(buffer.AsSpan(offset, count));
            }

            public override int Read(Span<byte> buffer)
            {
                if (_position >= _length)
                {
                    return 0;
                }

                var maxRead = (int)Math.Min(buffer.Length, _length - _position);
                var target = buffer[..maxRead];
                var read = RandomAccess.Read(_handle, target, _startOffset + _position);
                if (read <= 0)
                {
                    return 0;
                }

                var absolute = _startOffset + _position;
                for (var i = 0; i < read; i++)
                {
                    var keyIndex = (int)((absolute + i) % _xorKey.Length);
                    target[i] ^= _xorKey[keyIndex];
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

            public override void Flush()
            {
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

        private sealed record PakEntry(
            string Path,
            long Offset,
            long StoredSize,
            long OriginalSize,
            PakCompressionMethod Compression);

        private enum PakCompressionMethod : ushort
        {
            None = 0,
            DeflateFastest = 1,
        }
    }
}
