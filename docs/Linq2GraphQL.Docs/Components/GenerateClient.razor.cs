using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using Linq2GraphQL.Generator;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using TabBlazor;
using TabBlazor.Services;

namespace Linq2GraphQL.Docs.Components;

public partial class GenerateClient
{
    // Introspection results of large public schemas (GitHub) run to several MB.
    private const long MaxSchemaFileSize = 50 * 1024 * 1024;

    private readonly List<GenerateOptions> demoOptions = new();
    private bool isLoading;

    private GenerateOptions options = new();
    [Inject] private TablerService tablerService { get; set; }
    [Inject] private ToastService toastService { get; set; }
    [Inject] private IModalService modalService { get; set; }

    protected override void OnInitialized()
    {
        demoOptions.Add(new() { Namespace = "MyNamespace", ClientName = "MyClient" });

        demoOptions.Add(new()
        {
            Url = "https://swapi-graphql.netlify.app/graphql",
            Namespace = "StarWars.Client",
            ClientName = "StarWarsClient"
        });
        demoOptions.Add(new()
        {
            Url = "https://spacex-production.up.railway.app/", Namespace = "SpaceX", ClientName = "SpaceXClient"
        });
        demoOptions.Add(new()
        {
            Url = "https://api.github.com/graphql",
            Namespace = "Github",
            ClientName = "GithubClient",
            Token = "[Your Token]"
        });
        demoOptions.Add(new()
        {
            Url = "https://api.start.gg/gql/alpha",
            Namespace = "StartGG.Client",
            ClientName = "StartGGClient",
            Token = "[Your Token]"
        });

        //

        options = demoOptions.First();
    }

    private async Task CopyIntrospection()
    {
        string query;
        if (options.IncludeDeprecated)
        {
            query = General.IntrospectionQueryIncludeDeprecated;
        }
        else
        {
            query = General.IntrospectionQuery;
        }


        await tablerService.CopyToClipboard(query);
        await toastService.AddToastAsync(new()
        {
            Title = "Copy Complete", Message = "Introspection query has been copied to the clipboard"
        });
    }

    private async Task SaveEntriesAsync(List<FileEntry> entries)
    {
        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            foreach (var entry in entries)
            {
                var file = archive.CreateEntry(entry.DirectoryName + "/" + entry.FileName);
                using var entryStream = file.Open();
                using var streamWriter = new StreamWriter(entryStream);
                streamWriter.Write(entry.Content);
            }
        }

        memoryStream.Seek(0, SeekOrigin.Begin);
        await tablerService.SaveAsBinary($"{options.ClientName}.zip", "application/zip", memoryStream.ToArray());

        await toastService.AddToastAsync(new()
        {
            Title = "Generate Complete",
            Message = $"{options.ClientName}.zip has been been created! Please check you downloads."
        });
    }

    private async Task LoadSchemaFileAsync(InputFileChangeEventArgs e)
    {
        try
        {
            isLoading = true;
            await using var stream = e.File.OpenReadStream(MaxSchemaFileSize);
            using var reader = new StreamReader(stream);
            options.Schema = await reader.ReadToEndAsync();
        }
        catch (Exception ex)
        {
            var component = new RenderComponent<ExceptionModal>().Set(c => c.Exception, ex);
            await modalService.ShowAsync("Error", component, new() { Size = ModalSize.Large });
        }
        finally
        {
            isLoading = false;
        }
    }

    /// <summary>
    /// An introspection result is a JSON object; SDL can never start with '{'.
    /// </summary>
    private static bool IsIntrospectionJson(string schema) => schema?.TrimStart().StartsWith('{') == true;

    /// <summary>
    /// Where a schema was read from. A schema is reused only while all of these are unchanged; deprecated fields
    /// are part of what is read, so toggling them needs a new read too.
    /// </summary>
    private record SchemaSource(string Url, string Token, string Schema, bool IncludeDeprecated);

    private RootSchema loadedSchema;
    private SchemaSource loadedFrom;
    private List<ScalarInfo> scalars = new();

    private SchemaSource UrlSource => new(options.Url, options.Token, null, options.IncludeDeprecated);
    private SchemaSource TextSource => new(null, null, options.Schema, options.IncludeDeprecated);

    private bool IsLoaded => loadedSchema != null && (loadedFrom == UrlSource || loadedFrom == TextSource);

    private IEnumerable<ScalarInfo> CustomScalars => scalars.Where(e =>
        (options.ScalarOverrides.TryGetValue(e.Name, out var target) ? target : e.DefaultTarget ?? "") == "");

    private int OverrideCount => scalars.Count(e => options.ScalarOverrides.ContainsKey(e.Name));

    private Task LoadFromUrlAsync() => RunAsync(() => EnsureLoadedAsync(UrlSource));

    private Task LoadFromSchemaAsync() => RunAsync(() => EnsureLoadedAsync(TextSource));

    private Task GenerateClientAsync() => GenerateAsync(UrlSource);

    private Task GenerateClientFromSchema() => GenerateAsync(TextSource);

    private Task GenerateAsync(SchemaSource source) => RunAsync(async () =>
    {
        await EnsureLoadedAsync(source);
        await SaveEntriesAsync(CreateGenerator().Generate(loadedSchema));
    });

    private async Task EnsureLoadedAsync(SchemaSource source)
    {
        if (loadedSchema != null && loadedFrom == source)
        {
            return;
        }

        var reader = CreateGenerator();
        RootSchema schema;

        if (source.Url != null)
        {
            schema = await reader.ReadSchemaAsync(new(source.Url), source.Token);
        }
        else if (string.IsNullOrWhiteSpace(source.Schema))
        {
            throw new InvalidOperationException("Upload or paste a schema first.");
        }
        else
        {
            schema = IsIntrospectionJson(source.Schema)
                ? reader.ReadSchema(source.Schema)
                : reader.ReadSchemaFromSdl(source.Schema);
        }

        loadedSchema = schema;
        loadedFrom = source;
        scalars = schema.Data.Schema.GetScalars();
    }

    private async Task ReviewScalarsAsync()
    {
        var component = new RenderComponent<ScalarMappingsModal>()
            .Set(e => e.Scalars, scalars)
            .Set(e => e.Overrides, options.ScalarOverrides)
            .Set(e => e.ConfigJson, ConfigJson);
        await modalService.ShowAsync("Scalar mappings", component, new() { Size = ModalSize.Large });
    }

    private ClientGenerator CreateGenerator()
    {
        return new ClientGenerator(options.Namespace, options.ClientName, options.IncludeSubscriptions,
            options.EnumGeneratorStrategy, options.Nullable, options.IncludeDeprecated,
            Helpers.ResolveScalarMappings(ScalarMappings()), options.DescriptionAttributes, options.Agent,
            options.Standalone);
    }

    /// <summary>
    /// The overrides for the scalars of the loaded schema, as the config file's scalarMappings: an empty target
    /// becomes null, "generate a CustomScalar class". Overrides kept from another schema are left out.
    /// </summary>
    private Dictionary<string, string> ScalarMappings()
    {
        return scalars.Where(e => options.ScalarOverrides.ContainsKey(e.Name))
            .ToDictionary(e => e.Name, e => options.ScalarOverrides[e.Name] is "" ? null : options.ScalarOverrides[e.Name]);
    }

    /// <summary>
    /// The page's settings as a linq2graphql.json file for the command line, leaving out settings that have their
    /// default value. The token is never included.
    /// </summary>
    private string ConfigJson()
    {
        var mappings = ScalarMappings();
        var config = new GeneratorConfig
        {
            Endpoint = loadedFrom?.Url,
            Namespace = options.Namespace,
            Client = options.ClientName,
            Subscriptions = options.IncludeSubscriptions ? true : null,
            EnumStrategy = options.EnumGeneratorStrategy == EnumGeneratorStrategy.FailIfMissing
                ? null
                : options.EnumGeneratorStrategy.ToString(),
            Nullable = options.Nullable ? true : null,
            Deprecated = options.IncludeDeprecated ? true : null,
            Agent = options.Agent ? true : null,
            Standalone = options.Standalone ? true : null,
            DescriptionAttributes = options.DescriptionAttributes ? null : false,
            ScalarMappings = mappings.Count > 0 ? mappings : null
        };

        return JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true
        });
    }

    private async Task RunAsync(Func<Task> action)
    {
        try
        {
            isLoading = true;
            await action();
        }
        catch (Exception ex)
        {
            var component = new RenderComponent<ExceptionModal>().Set(e => e.Exception, ex);
            await modalService.ShowAsync("Error", component, new() { Size = ModalSize.Large });
        }
        finally
        {
            isLoading = false;
        }
    }
}

public class GenerateOptions
{
    public string Namespace { get; set; }
    public string ClientName { get; set; }
    public bool IncludeSubscriptions { get; set; }
    public bool Nullable { get; set; }
    public string Url { get; set; }
    public string Token { get; set; }
    public bool IncludeDeprecated { get; set; }
    public bool Standalone { get; set; }
    public bool Agent { get; set; }
    public bool DescriptionAttributes { get; set; } = true;
    public EnumGeneratorStrategy EnumGeneratorStrategy { get; set; }

    public string Schema { get; set; }

    /// <summary>
    /// Scalar name to C# type for the scalars whose mapping was changed in the review; an empty type means
    /// "generate a CustomScalar class". Kept per example, so a reloaded schema keeps its changes.
    /// </summary>
    public Dictionary<string, string> ScalarOverrides { get; set; } = new(StringComparer.InvariantCultureIgnoreCase);
}
