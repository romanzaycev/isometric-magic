namespace IonMotion.Engine.Core.Assets
{
    internal sealed class VirtualFileSystem : IDisposable
    {
        private readonly object _sync = new();
        private readonly List<MountedLayer> _layers = new();
        private readonly Dictionary<string, Func<Stream>> _resolvedEntries = new(StringComparer.Ordinal);
        private long _nextSequence;

        public void Reset()
        {
            lock (_sync)
            {
                foreach (var layer in _layers)
                {
                    layer.Dispose();
                }

                _layers.Clear();
                _resolvedEntries.Clear();
                _nextSequence = 0;
            }
        }

        public void MountDirectory(string directoryPath, int priority)
        {
            var fullDirectory = Path.GetFullPath(directoryPath);
            if (!Directory.Exists(fullDirectory))
            {
                return;
            }

            var files = Directory.GetFiles(fullDirectory, "*", SearchOption.AllDirectories);
            var entries = new Dictionary<string, Func<Stream>>(files.Length, StringComparer.Ordinal);
            foreach (var file in files)
            {
                var relative = Path.GetRelativePath(fullDirectory, file).Replace('\\', '/');
                var key = ResourcePath.NormalizeResourcePath($"{ResourcePath.EntryPoint}/{relative}");
                var fullPath = Path.GetFullPath(file);
                entries[key] = () => new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            }

            lock (_sync)
            {
                _layers.Add(MountedLayer.CreateDirectoryLayer(entries, priority, _nextSequence++));
                RebuildResolvedEntries();
            }
        }

        public void MountPak(string pakPath, int priority)
        {
            var archive = PakArchive.Open(pakPath);
            var entries = new Dictionary<string, Func<Stream>>(archive.Paths.Count, StringComparer.Ordinal);
            foreach (var path in archive.Paths)
            {
                var key = path;
                entries[key] = () => archive.OpenRead(key);
            }

            lock (_sync)
            {
                _layers.Add(MountedLayer.CreatePakLayer(archive, entries, priority, _nextSequence++));
                RebuildResolvedEntries();
            }
        }

        public bool Exists(string path)
        {
            var key = ResourcePath.NormalizeResourcePath(path);
            lock (_sync)
            {
                return _resolvedEntries.ContainsKey(key);
            }
        }

        public Stream OpenRead(string path)
        {
            var key = ResourcePath.NormalizeResourcePath(path);
            Func<Stream>? open;

            lock (_sync)
            {
                if (!_resolvedEntries.TryGetValue(key, out open))
                {
                    throw new FileNotFoundException($"Resource '{path}' not found.", path);
                }
            }

            return open();
        }

        public void Dispose()
        {
            Reset();
        }

        private void RebuildResolvedEntries()
        {
            _resolvedEntries.Clear();

            _layers.Sort(static (left, right) =>
            {
                var byPriority = left.Priority.CompareTo(right.Priority);
                if (byPriority != 0)
                {
                    return byPriority;
                }

                return left.Sequence.CompareTo(right.Sequence);
            });

            foreach (var layer in _layers)
            {
                foreach (var pair in layer.Entries)
                {
                    _resolvedEntries[pair.Key] = pair.Value;
                }
            }
        }

        private sealed class MountedLayer : IDisposable
        {
            private readonly IDisposable? _disposable;

            private MountedLayer(
                Dictionary<string, Func<Stream>> entries,
                int priority,
                long sequence,
                IDisposable? disposable)
            {
                Entries = entries;
                Priority = priority;
                Sequence = sequence;
                _disposable = disposable;
            }

            public Dictionary<string, Func<Stream>> Entries { get; }
            public int Priority { get; }
            public long Sequence { get; }

            public static MountedLayer CreateDirectoryLayer(
                Dictionary<string, Func<Stream>> entries,
                int priority,
                long sequence)
            {
                return new MountedLayer(entries, priority, sequence, null);
            }

            public static MountedLayer CreatePakLayer(
                PakArchive archive,
                Dictionary<string, Func<Stream>> entries,
                int priority,
                long sequence)
            {
                return new MountedLayer(entries, priority, sequence, archive);
            }

            public void Dispose()
            {
                _disposable?.Dispose();
            }
        }
    }
}
