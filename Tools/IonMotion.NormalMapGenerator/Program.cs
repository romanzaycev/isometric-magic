using System.Text.Json;
using System.Text.Json.Serialization;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

const float AlphaCutoff = 0.01f;

if (!TryParseInvocation(args, out var invocation, out var error))
{
    Console.Error.WriteLine(error);
    PrintUsage();
    return 1;
}

if (invocation.ProjectPath is not null)
{
    if (!TryBuildProjectJobs(invocation.ProjectPath, out var jobs, out error))
    {
        Console.Error.WriteLine(error);
        return 1;
    }

    Console.WriteLine($"Project: {invocation.ProjectPath}");
    Console.WriteLine($"Jobs: {jobs.Count}");

    for (var i = 0; i < jobs.Count; i++)
    {
        var job = jobs[i];
        Console.WriteLine($"[{i + 1}/{jobs.Count}] Input '{job.InputPath}'");
        if (!TryGenerate(job, out var generated, out var skipped, out error))
        {
            Console.Error.WriteLine(error);
            return 1;
        }

        Console.WriteLine($"Generated: {generated}, skipped existing: {skipped}");
    }

    Console.WriteLine("Project generation completed.");
    return 0;
}

if (!TryGenerate(invocation.SingleJob!, out var singleGenerated, out var singleSkipped, out error))
{
    Console.Error.WriteLine(error);
    return 1;
}

Console.WriteLine($"Generated: {singleGenerated}, skipped existing: {singleSkipped}");
return 0;

static bool TryGenerate(GenerationJob job, out int generated, out int skipped, out string error)
{
    generated = 0;
    skipped = 0;
    error = string.Empty;

    try
    {
        if (File.Exists(job.InputPath))
        {
            return TryGenerateSingle(job.InputPath, job, out generated, out skipped, out error);
        }

        if (!Directory.Exists(job.InputPath))
        {
            error = $"Input does not exist: {job.InputPath}";
            return false;
        }

        var files = Directory.GetFiles(job.InputPath, "*.*", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            if (!IsSupportedImage(file))
            {
                continue;
            }

            if (IsIgnoredSource(file, job.Suffix))
            {
                continue;
            }

            if (!TryGenerateSingle(file, job, out var oneGenerated, out var oneSkipped, out error))
            {
                return false;
            }

            generated += oneGenerated;
            skipped += oneSkipped;
        }

        return true;
    }
    catch (Exception ex)
    {
        error = $"Failed to generate normal maps for '{job.InputPath}': {ex.Message}";
        return false;
    }
}

static bool TryGenerateSingle(string sourcePath, GenerationJob job, out int generated, out int skipped, out string error)
{
    generated = 0;
    skipped = 0;
    error = string.Empty;

    if (!IsSupportedImage(sourcePath))
    {
        error = $"Unsupported input image format: {sourcePath}";
        return false;
    }

    if (IsIgnoredSource(sourcePath, job.Suffix))
    {
        error = $"Input image cannot be a layer file: {sourcePath}";
        return false;
    }

    var outputPath = BuildNormalPath(sourcePath, job.Suffix);
    if (!job.Overwrite && File.Exists(outputPath))
    {
        skipped = 1;
        return true;
    }

    using var source = Image.Load<Rgba32>(sourcePath);
    using var normal = GenerateNormalMap(source, job.Strength);

    var outputDirectory = Path.GetDirectoryName(outputPath)
        ?? throw new InvalidOperationException($"Failed to resolve output directory for '{outputPath}'.");
    Directory.CreateDirectory(outputDirectory);
    normal.Save(outputPath);

    Console.WriteLine($"Wrote: {outputPath}");
    generated = 1;
    return true;
}

static Image<Rgba32> GenerateNormalMap(Image<Rgba32> source, float strength)
{
    var width = source.Width;
    var height = source.Height;
    var pixelCount = width * height;

    var luminance = new float[pixelCount];
    var alpha = new float[pixelCount];

    for (var y = 0; y < height; y++)
    {
        for (var x = 0; x < width; x++)
        {
            var pixel = source[x, y];
            var idx = y * width + x;
            luminance[idx] = (0.2126f * pixel.R + 0.7152f * pixel.G + 0.0722f * pixel.B) / 255f;
            alpha[idx] = pixel.A / 255f;
        }
    }

    var output = new Image<Rgba32>(width, height);

    for (var y = 0; y < height; y++)
    {
        var y0 = Math.Max(y - 1, 0);
        var y1 = Math.Min(y + 1, height - 1);
        for (var x = 0; x < width; x++)
        {
            var x0 = Math.Max(x - 1, 0);
            var x1 = Math.Min(x + 1, width - 1);

            var idx = y * width + x;
            var aCenter = alpha[idx];
            var hCenter = luminance[idx] * aCenter;

            var tl = SampleHeight(luminance, alpha, width, x0, y0, hCenter);
            var t = SampleHeight(luminance, alpha, width, x, y0, hCenter);
            var tr = SampleHeight(luminance, alpha, width, x1, y0, hCenter);
            var l = SampleHeight(luminance, alpha, width, x0, y, hCenter);
            var r = SampleHeight(luminance, alpha, width, x1, y, hCenter);
            var bl = SampleHeight(luminance, alpha, width, x0, y1, hCenter);
            var b = SampleHeight(luminance, alpha, width, x, y1, hCenter);
            var br = SampleHeight(luminance, alpha, width, x1, y1, hCenter);

            var dx = (tr + 2f * r + br) - (tl + 2f * l + bl);
            var dy = (bl + 2f * b + br) - (tl + 2f * t + tr);

            dx *= strength / 8f;
            dy *= strength / 8f;

            var gradientAttenuation = aCenter;
            dx *= gradientAttenuation;
            dy *= gradientAttenuation;

            if (aCenter < AlphaCutoff)
            {
                output[x, y] = new Rgba32(128, 128, 255, 255);
                continue;
            }

            var nx = -dx;
            var ny = -dy;
            var nz = 1f;
            var len = (float)Math.Sqrt(nx * nx + ny * ny + nz * nz);
            nx /= len;
            ny /= len;
            nz /= len;

            output[x, y] = new Rgba32(
                (byte)Math.Clamp((nx * 0.5f + 0.5f) * 255f, 0f, 255f),
                (byte)Math.Clamp((ny * 0.5f + 0.5f) * 255f, 0f, 255f),
                (byte)Math.Clamp((nz * 0.5f + 0.5f) * 255f, 0f, 255f),
                255);
        }
    }

    return output;
}

static float SampleHeight(float[] luminance, float[] alpha, int width, int x, int y, float fallback)
{
    var idx = y * width + x;
    if (alpha[idx] < AlphaCutoff)
    {
        return fallback;
    }

    return luminance[idx] * alpha[idx];
}

static bool TryParseInvocation(string[] args, out InvocationOptions invocation, out string error)
{
    invocation = null!;
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
            || values.ContainsKey("--strength")
            || values.ContainsKey("--suffix")
            || values.ContainsKey("--overwrite"))
        {
            error = "--project cannot be combined with --input/--strength/--suffix/--overwrite.";
            return false;
        }

        var fullProjectPath = Path.GetFullPath(projectPath);
        if (!File.Exists(fullProjectPath))
        {
            error = $"Project file does not exist: {projectPath}";
            return false;
        }

        invocation = new InvocationOptions(null, fullProjectPath);
        return true;
    }

    if (!values.TryGetValue("--input", out var input))
    {
        error = "Missing required argument: --input";
        return false;
    }

    var fullInput = Path.GetFullPath(input);
    if (!File.Exists(fullInput) && !Directory.Exists(fullInput))
    {
        error = $"Input does not exist: {input}";
        return false;
    }

    var strength = ParsePositiveFloat(values, "--strength", 1f, out error);
    if (!string.IsNullOrEmpty(error))
    {
        return false;
    }

    var suffix = values.TryGetValue("--suffix", out var parsedSuffix)
        ? parsedSuffix
        : "_normal";
    if (!ValidateSuffix(suffix, "--suffix", out error))
    {
        return false;
    }

    var overwrite = ParseBool(values, "--overwrite", false, out error);
    if (!string.IsNullOrEmpty(error))
    {
        return false;
    }

    invocation = new InvocationOptions(new GenerationJob(fullInput, strength, suffix, overwrite), null);
    return true;
}

static bool TryBuildProjectJobs(string projectPath, out List<GenerationJob> jobs, out string error)
{
    jobs = new List<GenerationJob>();
    error = string.Empty;

    NormalMapProjectFile? project;
    try
    {
        project = JsonSerializer.Deserialize<NormalMapProjectFile>(File.ReadAllText(projectPath), new JsonSerializerOptions
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

    if (project.Jobs is null || project.Jobs.Count == 0)
    {
        error = "Project must define at least one job in 'jobs'.";
        return false;
    }

    var defaults = project.Defaults;
    var defaultStrength = defaults?.Strength ?? 1f;
    var defaultSuffix = defaults?.Suffix ?? "_normal";
    var defaultOverwrite = defaults?.Overwrite ?? false;

    if (!ValidatePositiveFloat(defaultStrength, "defaults.strength", out error)
        || !ValidateSuffix(defaultSuffix, "defaults.suffix", out error))
    {
        return false;
    }

    var projectDirectory = Path.GetDirectoryName(Path.GetFullPath(projectPath))
        ?? throw new InvalidOperationException("Failed to resolve project directory.");

    for (var i = 0; i < project.Jobs.Count; i++)
    {
        var job = project.Jobs[i];
        if (string.IsNullOrWhiteSpace(job.Input))
        {
            error = $"jobs[{i}].input is required.";
            return false;
        }

        var inputPath = ResolveProjectPath(projectDirectory, job.Input);
        if (!File.Exists(inputPath) && !Directory.Exists(inputPath))
        {
            error = $"Input does not exist for jobs[{i}]: {job.Input}";
            return false;
        }

        var strength = job.Strength ?? defaultStrength;
        var suffix = job.Suffix ?? defaultSuffix;
        var overwrite = job.Overwrite ?? defaultOverwrite;

        if (!ValidatePositiveFloat(strength, $"jobs[{i}].strength", out error)
            || !ValidateSuffix(suffix, $"jobs[{i}].suffix", out error))
        {
            return false;
        }

        jobs.Add(new GenerationJob(inputPath, strength, suffix, overwrite));
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

static float ParsePositiveFloat(IReadOnlyDictionary<string, string> values, string key, float defaultValue, out string error)
{
    error = string.Empty;
    if (!values.TryGetValue(key, out var raw))
    {
        return defaultValue;
    }

    if (!float.TryParse(raw, out var parsed) || parsed <= 0f)
    {
        error = $"Argument {key} must be a positive number.";
        return 0f;
    }

    return parsed;
}

static bool ParseBool(IReadOnlyDictionary<string, string> values, string key, bool defaultValue, out string error)
{
    error = string.Empty;
    if (!values.TryGetValue(key, out var raw))
    {
        return defaultValue;
    }

    if (bool.TryParse(raw, out var parsedBool))
    {
        return parsedBool;
    }

    if (raw == "1")
    {
        return true;
    }

    if (raw == "0")
    {
        return false;
    }

    error = $"Argument {key} must be true/false or 1/0.";
    return false;
}

static bool ValidatePositiveFloat(float value, string name, out string error)
{
    if (value <= 0f || float.IsNaN(value) || float.IsInfinity(value))
    {
        error = $"{name} must be a positive finite number.";
        return false;
    }

    error = string.Empty;
    return true;
}

static bool ValidateSuffix(string suffix, string name, out string error)
{
    if (string.IsNullOrWhiteSpace(suffix))
    {
        error = $"{name} must be a non-empty string.";
        return false;
    }

    if (suffix.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
        || suffix.Contains('/')
        || suffix.Contains('\\'))
    {
        error = $"{name} contains invalid file name characters.";
        return false;
    }

    error = string.Empty;
    return true;
}

static string BuildNormalPath(string sourcePath, string suffix)
{
    var directory = Path.GetDirectoryName(sourcePath)
        ?? throw new InvalidOperationException($"Failed to resolve directory for '{sourcePath}'.");
    var stem = Path.GetFileNameWithoutExtension(sourcePath);
    return Path.Combine(directory, $"{stem}{suffix}.png");
}

static bool IsSupportedImage(string path)
{
    var ext = Path.GetExtension(path);
    return ext.Equals(".png", StringComparison.OrdinalIgnoreCase)
           || ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
           || ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase);
}

static bool IsIgnoredSource(string sourcePath, string suffix)
{
    var stem = Path.GetFileNameWithoutExtension(sourcePath);
    return stem.EndsWith("_normal", StringComparison.OrdinalIgnoreCase)
           || stem.EndsWith("_emissive", StringComparison.OrdinalIgnoreCase)
           || stem.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
}

static void PrintUsage()
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run --project Tools/IonMotion.NormalMapGenerator -- --input <file|dir> [--strength 1.0] [--suffix _normal] [--overwrite false]");
    Console.WriteLine("  dotnet run --project Tools/IonMotion.NormalMapGenerator -- --project <project.json>");
    Console.WriteLine();
    Console.WriteLine("Example:");
    Console.WriteLine("  dotnet run --project Tools/IonMotion.NormalMapGenerator -- --input resources/data/textures/ts1");
    Console.WriteLine("  dotnet run --project Tools/IonMotion.NormalMapGenerator -- --input resources/data/textures/ts1/tree.png --strength 2");
    Console.WriteLine("  dotnet run --project Tools/IonMotion.NormalMapGenerator -- --project resources/pipeline/normalmaps/normalmap.project.json");
}

internal sealed record InvocationOptions(
    GenerationJob? SingleJob,
    string? ProjectPath);

internal sealed record GenerationJob(
    string InputPath,
    float Strength,
    string Suffix,
    bool Overwrite);

internal sealed record NormalMapProjectFile(
    [property: JsonPropertyName("version")] int? Version,
    [property: JsonPropertyName("defaults")] NormalMapProjectDefaults? Defaults,
    [property: JsonPropertyName("jobs")] List<NormalMapProjectJob>? Jobs);

internal sealed record NormalMapProjectDefaults(
    [property: JsonPropertyName("strength")] float? Strength,
    [property: JsonPropertyName("suffix")] string? Suffix,
    [property: JsonPropertyName("overwrite")] bool? Overwrite);

internal sealed record NormalMapProjectJob(
    [property: JsonPropertyName("input")] string? Input,
    [property: JsonPropertyName("strength")] float? Strength,
    [property: JsonPropertyName("suffix")] string? Suffix,
    [property: JsonPropertyName("overwrite")] bool? Overwrite);
