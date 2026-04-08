using System.Text.Json;
using System.Text.Json.Serialization;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

if (!TryParseInvocation(args, out var invocation, out var error))
{
    Console.Error.WriteLine(error);
    PrintUsage();
    return 1;
}

if (invocation.ProjectPath is not null)
{
    if (!TryBuildProjectOptions(invocation.ProjectPath, out var projectOptions, out error))
    {
        Console.Error.WriteLine(error);
        return 1;
    }

    Console.WriteLine($"Project: {Path.GetFullPath(invocation.ProjectPath)}");
    Console.WriteLine($"Atlases to pack: {projectOptions.Count}");

    for (var i = 0; i < projectOptions.Count; i++)
    {
        var projectOption = projectOptions[i];
        Console.WriteLine($"[{i + 1}/{projectOptions.Count}] Packing '{projectOption.OutputBasePath}' from '{projectOption.InputDirectory}'...");
        if (!TryPackAtlas(projectOption, out error))
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }

    Console.WriteLine("Project packing completed.");
    return 0;
}

if (!TryPackAtlas(invocation.SinglePackOptions!, out error))
{
    Console.Error.WriteLine(error);
    return 1;
}

return 0;

static bool TryPackAtlas(PackOptions options, out string error)
{
    error = string.Empty;
    try
    {
        var entries = DiscoverEntries(options.InputDirectory);
        if (entries.Count == 0)
        {
            error = $"No albedo images found in '{options.InputDirectory}'.";
            return false;
        }

        entries.Sort((a, b) => b.Height.CompareTo(a.Height));

        if (!TryPack(entries, options.MaxSize, options.Padding, options.Extrude, out var packedEntries, out var atlasWidth,
                out var atlasHeight, out var packError))
        {
            error = packError;
            return false;
        }

        var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(options.OutputBasePath))
            ?? throw new InvalidOperationException("Failed to resolve output directory.");
        Directory.CreateDirectory(outputDirectory);

        var outputBaseName = Path.GetFileName(options.OutputBasePath);
        var albedoFileName = $"{outputBaseName}.png";
        var normalFileName = $"{outputBaseName}_normal.png";
        var emissiveFileName = $"{outputBaseName}_emissive.png";
        var metadataFileName = $"{outputBaseName}.json";

        var albedoPath = Path.Combine(outputDirectory, albedoFileName);
        var normalPath = Path.Combine(outputDirectory, normalFileName);
        var emissivePath = Path.Combine(outputDirectory, emissiveFileName);
        var metadataPath = Path.Combine(outputDirectory, metadataFileName);

        var hasNormalLayer = entries.Exists(e => File.Exists(e.NormalPath));
        var hasEmissiveLayer = entries.Exists(e => File.Exists(e.EmissivePath));

        using var albedoAtlas = new Image<Rgba32>(atlasWidth, atlasHeight, new Rgba32(0, 0, 0, 0));
        using var normalAtlas = hasNormalLayer
            ? new Image<Rgba32>(atlasWidth, atlasHeight, new Rgba32(128, 128, 255, 255))
            : null;
        using var emissiveAtlas = hasEmissiveLayer
            ? new Image<Rgba32>(atlasWidth, atlasHeight, new Rgba32(0, 0, 0, 0))
            : null;

        foreach (var entry in packedEntries)
        {
            using var albedo = Image.Load<Rgba32>(entry.AlbedoPath);
            BlitWithExtrude(albedoAtlas, albedo, entry.X, entry.Y, options.Extrude);

            if (normalAtlas != null)
            {
                if (File.Exists(entry.NormalPath))
                {
                    using var normal = Image.Load<Rgba32>(entry.NormalPath);
                    ValidateLayerSize(entry, normal.Width, normal.Height, "normal");
                    BlitWithExtrude(normalAtlas, normal, entry.X, entry.Y, options.Extrude);
                }
                else
                {
                    FillWithExtrude(normalAtlas, entry.X, entry.Y, entry.Width, entry.Height, options.Extrude,
                        new Rgba32(128, 128, 255, 255));
                }
            }

            if (emissiveAtlas != null)
            {
                if (File.Exists(entry.EmissivePath))
                {
                    using var emissive = Image.Load<Rgba32>(entry.EmissivePath);
                    ValidateLayerSize(entry, emissive.Width, emissive.Height, "emissive");
                    BlitWithExtrude(emissiveAtlas, emissive, entry.X, entry.Y, options.Extrude);
                }
                else
                {
                    FillWithExtrude(emissiveAtlas, entry.X, entry.Y, entry.Width, entry.Height, options.Extrude,
                        new Rgba32(0, 0, 0, 0));
                }
            }
        }

        albedoAtlas.Save(albedoPath);
        normalAtlas?.Save(normalPath);
        emissiveAtlas?.Save(emissivePath);

        var metadata = BuildMetadata(packedEntries, atlasWidth, atlasHeight, options.Padding, options.Extrude,
            albedoFileName,
            hasNormalLayer ? normalFileName : null,
            hasEmissiveLayer ? emissiveFileName : null);

        var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        });
        File.WriteAllText(metadataPath, json);

        Console.WriteLine($"Packed {packedEntries.Count} regions.");
        Console.WriteLine($"Atlas size: {atlasWidth}x{atlasHeight}");
        Console.WriteLine($"Wrote: {albedoPath}");
        if (hasNormalLayer)
        {
            Console.WriteLine($"Wrote: {normalPath}");
        }

        if (hasEmissiveLayer)
        {
            Console.WriteLine($"Wrote: {emissivePath}");
        }

        Console.WriteLine($"Wrote: {metadataPath}");
        return true;
    }
    catch (Exception ex)
    {
        error = $"Failed to pack atlas for output '{options.OutputBasePath}': {ex.Message}";
        return false;
    }
}

static bool TryParseInvocation(string[] args, out InvocationOptions options, out string error)
{
    options = null!;
    error = string.Empty;

    var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    for (var i = 0; i < args.Length; i++)
    {
        var arg = args[i];
        if (!arg.StartsWith("--", StringComparison.Ordinal))
        {
            error = $"Unexpected argument: {arg}";
            return false;
        }

        if (i + 1 >= args.Length)
        {
            error = $"Missing value for argument: {arg}";
            return false;
        }

        values[arg] = args[i + 1];
        i++;
    }

    if (values.TryGetValue("--project", out var projectPath))
    {
        if (values.ContainsKey("--input")
            || values.ContainsKey("--out")
            || values.ContainsKey("--maxSize")
            || values.ContainsKey("--padding")
            || values.ContainsKey("--extrude"))
        {
            error = "--project cannot be combined with --input/--out/--maxSize/--padding/--extrude.";
            return false;
        }

        var fullProjectPath = Path.GetFullPath(projectPath);
        if (!File.Exists(fullProjectPath))
        {
            error = $"Project file does not exist: {projectPath}";
            return false;
        }

        options = new InvocationOptions(null, fullProjectPath);
        return true;
    }

    if (!values.TryGetValue("--input", out var input))
    {
        error = "Missing required argument: --input";
        return false;
    }

    if (!values.TryGetValue("--out", out var output))
    {
        error = "Missing required argument: --out";
        return false;
    }

    if (!Directory.Exists(input))
    {
        error = $"Input directory does not exist: {input}";
        return false;
    }

    var maxSize = ParsePositiveInt(values, "--maxSize", 2048, out error);
    if (!string.IsNullOrEmpty(error))
    {
        return false;
    }

    var padding = ParseNonNegativeInt(values, "--padding", 2, out error);
    if (!string.IsNullOrEmpty(error))
    {
        return false;
    }

    var extrude = ParseNonNegativeInt(values, "--extrude", 1, out error);
    if (!string.IsNullOrEmpty(error))
    {
        return false;
    }

    var packOptions = new PackOptions(
        Path.GetFullPath(input),
        Path.GetFullPath(output),
        maxSize,
        padding,
        extrude);
    options = new InvocationOptions(packOptions, null);
    return true;
}

static bool TryBuildProjectOptions(string projectPath, out List<PackOptions> options, out string error)
{
    options = new List<PackOptions>();
    error = string.Empty;

    AtlasProjectFile? project;
    try
    {
        project = JsonSerializer.Deserialize<AtlasProjectFile>(File.ReadAllText(projectPath), new JsonSerializerOptions
        {
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            PropertyNameCaseInsensitive = true,
        });
    }
    catch (JsonException ex)
    {
        error = $"Invalid project JSON '{projectPath}': {ex.Message}";
        return false;
    }

    if (project is null)
    {
        error = $"Project file '{projectPath}' is empty or invalid.";
        return false;
    }

    var version = project.Version ?? 1;
    if (version != 1)
    {
        error = $"Unsupported project version '{version}'. Expected 1.";
        return false;
    }

    if (project.Atlases is null || project.Atlases.Count == 0)
    {
        error = "Project must define at least one atlas in 'atlases'.";
        return false;
    }

    var defaults = project.Defaults;
    var defaultMaxSize = defaults?.MaxSize ?? 2048;
    var defaultPadding = defaults?.Padding ?? 2;
    var defaultExtrude = defaults?.Extrude ?? 1;

    if (!ValidatePositiveInt(defaultMaxSize, "defaults.maxSize", out error)
        || !ValidateNonNegativeInt(defaultPadding, "defaults.padding", out error)
        || !ValidateNonNegativeInt(defaultExtrude, "defaults.extrude", out error))
    {
        return false;
    }

    var projectDirectory = Path.GetDirectoryName(Path.GetFullPath(projectPath))
        ?? throw new InvalidOperationException("Failed to resolve project directory.");

    for (var i = 0; i < project.Atlases.Count; i++)
    {
        var atlas = project.Atlases[i];

        if (string.IsNullOrWhiteSpace(atlas.Input))
        {
            error = $"atlases[{i}].input is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(atlas.Out))
        {
            error = $"atlases[{i}].out is required.";
            return false;
        }

        var maxSize = atlas.MaxSize ?? defaultMaxSize;
        var padding = atlas.Padding ?? defaultPadding;
        var extrude = atlas.Extrude ?? defaultExtrude;

        if (!ValidatePositiveInt(maxSize, $"atlases[{i}].maxSize", out error)
            || !ValidateNonNegativeInt(padding, $"atlases[{i}].padding", out error)
            || !ValidateNonNegativeInt(extrude, $"atlases[{i}].extrude", out error))
        {
            return false;
        }

        var inputDirectory = ResolveProjectPath(projectDirectory, atlas.Input);
        if (!Directory.Exists(inputDirectory))
        {
            error = $"Input directory does not exist for atlases[{i}]: {atlas.Input}";
            return false;
        }

        var outputBasePath = ResolveProjectPath(projectDirectory, atlas.Out);
        options.Add(new PackOptions(inputDirectory, outputBasePath, maxSize, padding, extrude));
    }

    return true;
}

static string ResolveProjectPath(string projectDirectory, string value)
{
    if (Path.IsPathRooted(value))
    {
        return Path.GetFullPath(value);
    }

    return Path.GetFullPath(Path.Combine(projectDirectory, value));
}

static bool ValidatePositiveInt(int value, string name, out string error)
{
    if (value <= 0)
    {
        error = $"{name} must be a positive integer.";
        return false;
    }

    error = string.Empty;
    return true;
}

static bool ValidateNonNegativeInt(int value, string name, out string error)
{
    if (value < 0)
    {
        error = $"{name} must be a non-negative integer.";
        return false;
    }

    error = string.Empty;
    return true;
}

static int ParsePositiveInt(IReadOnlyDictionary<string, string> values, string key, int defaultValue, out string error)
{
    error = string.Empty;
    if (!values.TryGetValue(key, out var value))
    {
        return defaultValue;
    }

    if (!int.TryParse(value, out var parsed) || parsed <= 0)
    {
        error = $"Argument {key} must be a positive integer.";
        return 0;
    }

    return parsed;
}

static int ParseNonNegativeInt(IReadOnlyDictionary<string, string> values, string key, int defaultValue,
    out string error)
{
    error = string.Empty;
    if (!values.TryGetValue(key, out var value))
    {
        return defaultValue;
    }

    if (!int.TryParse(value, out var parsed) || parsed < 0)
    {
        error = $"Argument {key} must be a non-negative integer.";
        return 0;
    }

    return parsed;
}

static List<SourceEntry> DiscoverEntries(string inputDirectory)
{
    var entries = new List<SourceEntry>();
    var files = Directory.GetFiles(inputDirectory, "*.*", SearchOption.AllDirectories);

    foreach (var file in files)
    {
        if (!IsSupportedImage(file))
        {
            continue;
        }

        var withoutExtension = Path.GetFileNameWithoutExtension(file);
        if (withoutExtension.EndsWith("_normal", StringComparison.OrdinalIgnoreCase)
            || withoutExtension.EndsWith("_emissive", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        var relative = Path.GetRelativePath(inputDirectory, file).Replace('\\', '/');
        var imageInfo = Image.Identify(file)
            ?? throw new InvalidOperationException($"Failed to identify image file: {file}");

        var normalPath = BuildLayerPath(file, "_normal");
        var emissivePath = BuildLayerPath(file, "_emissive");

        entries.Add(new SourceEntry(file, normalPath, emissivePath, relative, imageInfo.Width, imageInfo.Height));
    }

    return entries;
}

static bool TryPack(
    IReadOnlyList<SourceEntry> entries,
    int maxSize,
    int padding,
    int extrude,
    out List<PackedEntry> packedEntries,
    out int atlasWidth,
    out int atlasHeight,
    out string error)
{
    packedEntries = new List<PackedEntry>(entries.Count);
    atlasWidth = 0;
    atlasHeight = 0;
    error = string.Empty;

    var x = padding;
    var y = padding;
    var rowHeight = 0;
    var maxRight = 0;
    var maxBottom = 0;

    foreach (var entry in entries)
    {
        var packedWidth = entry.Width + extrude * 2;
        var packedHeight = entry.Height + extrude * 2;

        if (packedWidth + padding * 2 > maxSize || packedHeight + padding * 2 > maxSize)
        {
            error = $"Image '{entry.Key}' ({entry.Width}x{entry.Height}) does not fit into max atlas size {maxSize}.";
            return false;
        }

        if (x + packedWidth + padding > maxSize)
        {
            x = padding;
            y += rowHeight + padding;
            rowHeight = 0;
        }

        if (y + packedHeight + padding > maxSize)
        {
            error = $"Atlas overflow: increase --maxSize for '{entry.Key}'.";
            return false;
        }

        var regionX = x + extrude;
        var regionY = y + extrude;
        packedEntries.Add(new PackedEntry(
            entry.AlbedoPath,
            entry.NormalPath,
            entry.EmissivePath,
            entry.Key,
            regionX,
            regionY,
            entry.Width,
            entry.Height));

        maxRight = Math.Max(maxRight, x + packedWidth);
        maxBottom = Math.Max(maxBottom, y + packedHeight);

        x += packedWidth + padding;
        rowHeight = Math.Max(rowHeight, packedHeight);
    }

    atlasWidth = maxRight + padding;
    atlasHeight = maxBottom + padding;
    return true;
}

static AtlasMetadata BuildMetadata(
    IReadOnlyList<PackedEntry> packedEntries,
    int atlasWidth,
    int atlasHeight,
    int padding,
    int extrude,
    string albedoFileName,
    string? normalFileName,
    string? emissiveFileName)
{
    var regions = new Dictionary<string, AtlasRegionMetadata>(StringComparer.Ordinal);
    foreach (var entry in packedEntries)
    {
        regions[entry.Key] = new AtlasRegionMetadata(entry.X, entry.Y, entry.Width, entry.Height);
    }

    return new AtlasMetadata(
        1,
        new AtlasSizeMetadata(atlasWidth, atlasHeight),
        new AtlasLayersMetadata(albedoFileName, normalFileName, emissiveFileName),
        padding,
        extrude,
        regions);
}

static void BlitWithExtrude(Image<Rgba32> destination, Image<Rgba32> source, int x, int y, int extrude)
{
    Blit(destination, source, x, y);
    if (extrude <= 0)
    {
        return;
    }

    var width = source.Width;
    var height = source.Height;

    for (var i = 1; i <= extrude; i++)
    {
        for (var dx = 0; dx < width; dx++)
        {
            destination[x + dx, y - i] = source[dx, 0];
            destination[x + dx, y + height - 1 + i] = source[dx, height - 1];
        }

        for (var dy = 0; dy < height; dy++)
        {
            destination[x - i, y + dy] = source[0, dy];
            destination[x + width - 1 + i, y + dy] = source[width - 1, dy];
        }
    }

    var topLeft = source[0, 0];
    var topRight = source[width - 1, 0];
    var bottomLeft = source[0, height - 1];
    var bottomRight = source[width - 1, height - 1];

    for (var i = 1; i <= extrude; i++)
    {
        for (var j = 1; j <= extrude; j++)
        {
            destination[x - i, y - j] = topLeft;
            destination[x + width - 1 + i, y - j] = topRight;
            destination[x - i, y + height - 1 + j] = bottomLeft;
            destination[x + width - 1 + i, y + height - 1 + j] = bottomRight;
        }
    }
}

static void FillWithExtrude(Image<Rgba32> destination, int x, int y, int width, int height, int extrude, Rgba32 color)
{
    for (var yy = y; yy < y + height; yy++)
    {
        for (var xx = x; xx < x + width; xx++)
        {
            destination[xx, yy] = color;
        }
    }

    if (extrude <= 0)
    {
        return;
    }

    for (var i = 1; i <= extrude; i++)
    {
        for (var dx = 0; dx < width; dx++)
        {
            destination[x + dx, y - i] = color;
            destination[x + dx, y + height - 1 + i] = color;
        }

        for (var dy = 0; dy < height; dy++)
        {
            destination[x - i, y + dy] = color;
            destination[x + width - 1 + i, y + dy] = color;
        }
    }

    for (var i = 1; i <= extrude; i++)
    {
        for (var j = 1; j <= extrude; j++)
        {
            destination[x - i, y - j] = color;
            destination[x + width - 1 + i, y - j] = color;
            destination[x - i, y + height - 1 + j] = color;
            destination[x + width - 1 + i, y + height - 1 + j] = color;
        }
    }
}

static void Blit(Image<Rgba32> destination, Image<Rgba32> source, int x, int y)
{
    for (var yy = 0; yy < source.Height; yy++)
    {
        for (var xx = 0; xx < source.Width; xx++)
        {
            destination[x + xx, y + yy] = source[xx, yy];
        }
    }
}

static string BuildLayerPath(string albedoPath, string suffix)
{
    var directory = Path.GetDirectoryName(albedoPath)
        ?? throw new InvalidOperationException($"Failed to resolve directory for: {albedoPath}");
    var stem = Path.GetFileNameWithoutExtension(albedoPath);
    return Path.Combine(directory, $"{stem}{suffix}.png");
}

static bool IsSupportedImage(string path)
{
    var ext = Path.GetExtension(path);
    return ext.Equals(".png", StringComparison.OrdinalIgnoreCase)
           || ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
           || ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase);
}

static void ValidateLayerSize(PackedEntry entry, int width, int height, string layerName)
{
    if (width != entry.Width || height != entry.Height)
    {
        throw new InvalidOperationException(
            $"{layerName} layer size mismatch for '{entry.Key}': expected {entry.Width}x{entry.Height}, got {width}x{height}.");
    }
}

static void PrintUsage()
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run --project Tools/IsometricMagic.AtlasPacker -- --input <dir> --out <basePath> [--maxSize 2048] [--padding 2] [--extrude 1]");
    Console.WriteLine("  dotnet run --project Tools/IsometricMagic.AtlasPacker -- --project <project.json>");
    Console.WriteLine();
    Console.WriteLine("Example:");
    Console.WriteLine("  dotnet run --project Tools/IsometricMagic.AtlasPacker -- --input resources/data/textures/ts1 --out resources/data/atlases/ts1");
    Console.WriteLine("  dotnet run --project Tools/IsometricMagic.AtlasPacker -- --project resources/data/atlases/pack.project.json");
}

internal sealed record InvocationOptions(
    PackOptions? SinglePackOptions,
    string? ProjectPath);

internal sealed record PackOptions(
    string InputDirectory,
    string OutputBasePath,
    int MaxSize,
    int Padding,
    int Extrude);

internal sealed record AtlasProjectFile(
    [property: JsonPropertyName("version")] int? Version,
    [property: JsonPropertyName("defaults")] AtlasProjectDefaults? Defaults,
    [property: JsonPropertyName("atlases")] List<AtlasProjectEntry>? Atlases);

internal sealed record AtlasProjectDefaults(
    [property: JsonPropertyName("maxSize")] int? MaxSize,
    [property: JsonPropertyName("padding")] int? Padding,
    [property: JsonPropertyName("extrude")] int? Extrude);

internal sealed record AtlasProjectEntry(
    [property: JsonPropertyName("input")] string? Input,
    [property: JsonPropertyName("out")] string? Out,
    [property: JsonPropertyName("maxSize")] int? MaxSize,
    [property: JsonPropertyName("padding")] int? Padding,
    [property: JsonPropertyName("extrude")] int? Extrude);

internal sealed record SourceEntry(
    string AlbedoPath,
    string NormalPath,
    string EmissivePath,
    string Key,
    int Width,
    int Height);

internal sealed record PackedEntry(
    string AlbedoPath,
    string NormalPath,
    string EmissivePath,
    string Key,
    int X,
    int Y,
    int Width,
    int Height);

internal sealed record AtlasMetadata(
    [property: JsonPropertyName("version")] int Version,
    [property: JsonPropertyName("size")] AtlasSizeMetadata Size,
    [property: JsonPropertyName("layers")] AtlasLayersMetadata Layers,
    [property: JsonPropertyName("padding")] int Padding,
    [property: JsonPropertyName("extrude")] int Extrude,
    [property: JsonPropertyName("regions")] Dictionary<string, AtlasRegionMetadata> Regions);

internal sealed record AtlasSizeMetadata(
    [property: JsonPropertyName("w")] int Width,
    [property: JsonPropertyName("h")] int Height);

internal sealed record AtlasLayersMetadata(
    [property: JsonPropertyName("albedo")] string Albedo,
    [property: JsonPropertyName("normal")] string? Normal,
    [property: JsonPropertyName("emissive")] string? Emissive);

internal sealed record AtlasRegionMetadata(
    [property: JsonPropertyName("x")] int X,
    [property: JsonPropertyName("y")] int Y,
    [property: JsonPropertyName("w")] int Width,
    [property: JsonPropertyName("h")] int Height);
