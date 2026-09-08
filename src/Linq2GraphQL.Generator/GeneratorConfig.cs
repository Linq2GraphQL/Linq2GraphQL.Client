using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Linq2GraphQL.Generator;

/// <summary>
/// The settings file (<c>--config</c>). Every value is optional and acts as the baseline for the
/// command line: an explicitly passed flag always wins over the corresponding value here.
/// </summary>
public class GeneratorConfig
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>
    /// The settings this file understands, derived from the properties below so the two cannot fall
    /// out of sync. Compared case insensitively, like the deserializer itself.
    /// </summary>
    private static readonly HashSet<string> KnownSettings =
        typeof(GeneratorConfig)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(e => e.CanWrite)
            .Select(e => JsonNamingPolicy.CamelCase.ConvertName(e.Name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public string Endpoint { get; set; }

    public string Output { get; set; }

    public string Namespace { get; set; }

    public string Client { get; set; }

    public string Token { get; set; }

    public bool? Subscriptions { get; set; }

    public string EnumStrategy { get; set; }

    public bool? Nullable { get; set; }

    public bool? Deprecated { get; set; }

    /// <summary>
    /// GraphQL scalar name to simple CLR type, e.g. <c>"DateTime": "System.DateTime"</c>. The
    /// target must be one of <see cref="Helpers.SupportedTargetTypeNames"/>. A null value opts the
    /// scalar out of the built-in mapping, so a CustomScalar class is generated for it instead.
    /// </summary>
    public Dictionary<string, string> ScalarMappings { get; set; }

    /// <summary>
    /// The file name looked for in the current directory when no --config is passed.
    /// </summary>
    public const string DefaultFileName = "linq2graphql.json";

    /// <summary>
    /// The explicit --config file when one is given, otherwise a <see cref="DefaultFileName"/> in
    /// the working directory if there is one, otherwise an empty config. An explicit path that does
    /// not exist is an error; a missing default file is not.
    /// </summary>
    public static GeneratorConfig Resolve(string configPath, string workingDirectory = null)
    {
        if (!string.IsNullOrWhiteSpace(configPath))
        {
            return Load(configPath);
        }

        var discovered = Path.Combine(workingDirectory ?? Environment.CurrentDirectory, DefaultFileName);

        return File.Exists(discovered) ? Load(discovered) : new GeneratorConfig();
    }

    public static GeneratorConfig Load(string path)
    {
        var fullPath = Path.GetFullPath(path, Environment.CurrentDirectory);

        if (!File.Exists(fullPath))
        {
            throw new GeneratorConfigurationException($"Config file not found: {fullPath}");
        }

        Console.WriteLine($"Reading configuration from {fullPath}");

        var json = File.ReadAllText(fullPath);

        GeneratorConfig config;
        try
        {
            ValidateKnownSettings(json, fullPath);
            config = JsonSerializer.Deserialize<GeneratorConfig>(json, SerializerOptions);
        }
        catch (JsonException ex)
        {
            throw new GeneratorConfigurationException($"Config file {fullPath} is not valid JSON: {ex.Message}", ex);
        }

        return config ?? new GeneratorConfig();
    }

    /// <summary>
    /// Rejects a setting we do not understand. System.Text.Json drops unmapped members silently, so
    /// without this a typo like "scalarMapping" would be ignored and the run would quietly produce
    /// the wrong client. Only top level settings are checked - the keys inside
    /// <see cref="ScalarMappings"/> are scalar names, which we cannot know up front.
    /// </summary>
    private static void ValidateKnownSettings(string json, string fullPath)
    {
        using var document = JsonDocument.Parse(json,
            new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });

        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new GeneratorConfigurationException($"Config file {fullPath} must contain a json object.");
        }

        var unknown = document.RootElement.EnumerateObject()
            .Select(e => e.Name)
            .Where(e => !KnownSettings.Contains(e))
            .ToList();

        if (unknown.Count == 0)
        {
            return;
        }

        throw new GeneratorConfigurationException(
            $"Unknown setting{(unknown.Count == 1 ? "" : "s")} in {fullPath}: {string.Join(", ", unknown)}. " +
            $"Supported settings are: {string.Join(", ", KnownSettings.OrderBy(e => e, StringComparer.Ordinal))}.");
    }
}
