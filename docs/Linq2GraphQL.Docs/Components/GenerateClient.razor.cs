using System.IO.Compression;
using Linq2GraphQL.Generator;
using Microsoft.AspNetCore.Components;
using TabBlazor;
using TabBlazor.Services;

namespace Linq2GraphQL.Docs.Components;

public partial class GenerateClient
{
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

    private async Task GenerateClientJson()
    {
        try
        {
            isLoading = true;
            var generator = new ClientGenerator(options.Namespace, options.ClientName, options.IncludeSubscriptions,
                EnumGeneratorStrategy.FailIfMissing, options.Nullable, options.IncludeDeprecated);
            var entries = generator.Generate(options.Schema);
            await SaveEntriesAsync(entries);
        }
        catch (Exception ex)
        {
            var component = new RenderComponent<ExceptionModal>().Set(e => e.Exception, ex);
            var result = await modalService.ShowAsync("Error", component, new() { Size = ModalSize.Large });
        }
        finally
        {
            isLoading = false;
        }
    }


    private async Task GenerateClientAsync()
    {
        try
        {
            isLoading = true;
            var generator = new ClientGenerator(options.Namespace, options.ClientName, options.IncludeSubscriptions,
                options.EnumGeneratorStrategy, options.Nullable, options.IncludeDeprecated);
            var entries = await generator.GenerateAsync(new(options.Url), options.Token);
            await SaveEntriesAsync(entries);
        }
        catch (Exception ex)
        {
            var component = new RenderComponent<ExceptionModal>().Set(e => e.Exception, ex);
            var result = await modalService.ShowAsync("Error", component, new() { Size = ModalSize.Large });
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
    public EnumGeneratorStrategy EnumGeneratorStrategy { get; set; }

    public string Schema { get; set; }
}