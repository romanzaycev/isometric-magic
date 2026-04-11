namespace IsometricMagic.Engine.Assets
{
    internal static class ResourcePath
    {
        public const string EntryPoint = "resources";
        private const string EntryPrefix = EntryPoint + "/";

        public static bool IsFileSystemAbsolutePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            return Path.IsPathRooted(path.Trim());
        }

        public static bool TryNormalizeResourcePath(string path, out string normalized)
        {
            try
            {
                normalized = NormalizeResourcePath(path);
                return true;
            }
            catch
            {
                normalized = string.Empty;
                return false;
            }
        }

        public static string Normalize(string path)
        {
            return NormalizeResourcePath(path);
        }

        public static string NormalizeResourcePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path cannot be null or whitespace.", nameof(path));
            }

            var value = path.Trim().Replace('\\', '/');

            while (value.StartsWith("./", StringComparison.Ordinal))
            {
                value = value[2..];
            }

            if (Path.IsPathRooted(value))
            {
                if (!TryExtractResourceRelativeFromAbsolute(value, out var resourceRelative))
                {
                    throw new InvalidOperationException(
                        $"Absolute path '{path}' is outside the '{EntryPoint}' entry point.");
                }

                value = resourceRelative;
            }

            value = value.TrimStart('/');

            if (!value.StartsWith(EntryPrefix, StringComparison.OrdinalIgnoreCase)
                && !value.Equals(EntryPoint, StringComparison.OrdinalIgnoreCase))
            {
                value = $"{EntryPoint}/{value}";
            }

            value = CollapseSegments(value);

            if (!value.StartsWith(EntryPrefix, StringComparison.OrdinalIgnoreCase)
                && !value.Equals(EntryPoint, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Path '{path}' does not resolve under '{EntryPoint}'.");
            }

            return value.ToLowerInvariant();
        }

        public static string ResolveFromFile(string referrerFilePath, string referencedPath)
        {
            if (string.IsNullOrWhiteSpace(referrerFilePath))
            {
                throw new ArgumentException("Referrer file path cannot be null or whitespace.", nameof(referrerFilePath));
            }

            if (string.IsNullOrWhiteSpace(referencedPath))
            {
                throw new ArgumentException("Referenced path cannot be null or whitespace.", nameof(referencedPath));
            }

            var reference = NormalizeSeparators(referencedPath).Trim();
            if (IsResourceAbsoluteOrFileSystemAbsolute(reference))
            {
                return NormalizeResourcePath(reference);
            }

            var baseDirectory = GetDirectoryName(referrerFilePath);
            return NormalizeResourcePath($"{baseDirectory}/{reference}");
        }

        public static string ResolveFromDirectory(string baseDirectoryPath, string referencedPath)
        {
            if (string.IsNullOrWhiteSpace(baseDirectoryPath))
            {
                throw new ArgumentException("Base directory path cannot be null or whitespace.", nameof(baseDirectoryPath));
            }

            if (string.IsNullOrWhiteSpace(referencedPath))
            {
                throw new ArgumentException("Referenced path cannot be null or whitespace.", nameof(referencedPath));
            }

            var reference = NormalizeSeparators(referencedPath).Trim();
            if (IsResourceAbsoluteOrFileSystemAbsolute(reference))
            {
                return NormalizeResourcePath(reference);
            }

            var directory = NormalizeResourcePath(baseDirectoryPath);
            return NormalizeResourcePath($"{directory}/{reference}");
        }

        public static string GetDirectoryName(string resourceFilePath)
        {
            var normalized = NormalizeResourcePath(resourceFilePath);
            var slash = normalized.LastIndexOf('/');
            if (slash <= 0)
            {
                return EntryPoint;
            }

            return normalized[..slash];
        }

        private static bool TryExtractResourceRelativeFromAbsolute(string absolutePath, out string resourcePath)
        {
            var normalized = Path.GetFullPath(absolutePath);
            normalized = NormalizeSeparators(normalized);
            var marker = $"/{EntryPoint}/";
            var markerIndex = normalized.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex >= 0)
            {
                resourcePath = normalized[(markerIndex + 1)..];
                return true;
            }

            if (normalized.EndsWith($"/{EntryPoint}", StringComparison.OrdinalIgnoreCase))
            {
                resourcePath = EntryPoint;
                return true;
            }

            resourcePath = string.Empty;
            return false;
        }

        private static bool IsResourceAbsoluteOrFileSystemAbsolute(string path)
        {
            if (Path.IsPathRooted(path))
            {
                return true;
            }

            if (path.Equals(EntryPoint, StringComparison.OrdinalIgnoreCase)
                || path.StartsWith(EntryPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (path.Equals($"./{EntryPoint}", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith($"./{EntryPrefix}", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private static string NormalizeSeparators(string path)
        {
            return path.Replace('\\', '/');
        }

        private static string CollapseSegments(string path)
        {
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var stack = new List<string>(segments.Length);

            foreach (var segment in segments)
            {
                if (segment == ".")
                {
                    continue;
                }

                if (segment == "..")
                {
                    if (stack.Count <= 1)
                    {
                        throw new InvalidOperationException($"Path '{path}' escapes '{EntryPoint}'.");
                    }

                    stack.RemoveAt(stack.Count - 1);
                    continue;
                }

                stack.Add(segment);
            }

            if (stack.Count == 0)
            {
                return EntryPoint;
            }

            return string.Join('/', stack);
        }
    }
}
