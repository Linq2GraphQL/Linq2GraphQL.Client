using Microsoft.AspNetCore.Components;
using System.IO.Compression;
using TabBlazor;
using TabBlazor.Services;

namespace Linq2GraphQL.Docs.Components
{

    public partial class GenerateClient
    {
        [Inject] private TablerService tablerService { get; set; }
        [Inject] private IModalService modalService { get; set; }

        private GenerateOptions options = new();

        private List<GenerateOptions> demoOptions = new();

        protected override void OnInitialized()
        {
            demoOptions.Add(new GenerateOptions
            {
                Namespace = "MyNamespace",
                ClientName = "MyClient"
            });

            demoOptions.Add(new GenerateOptions
            {
                Url = "https://swapi-graphql.netlify.app/.netlify/functions/index",
                Namespace = "StarWars.Client",
                ClientName = "StarWarsClient"
            });
            demoOptions.Add(new GenerateOptions
            {
                Url = "https://spacex-production.up.railway.app/",
                Namespace = "SpaceX",
                ClientName = "SpaceXClient"
            });
            demoOptions.Add(new GenerateOptions
            {
                Url = "https://api.github.com/graphql",
                Namespace = "Github",
                ClientName = "GithubClient",
                Token = "[Your Token]"
            });

            //

            options = demoOptions.First();
        }

        private async Task GenerateClientAsync()
        {
            try
            {


                var generator = new Generator.ClientGenerator(options.Namespace, options.ClientName, options.IncludeSubscriptions);
                var enries = await generator.GenerateAsync(new Uri(options.Url), options.Token);

                using var memoryStream = new MemoryStream();
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    foreach (var entry in enries)
                    {
                        var file = archive.CreateEntry(entry.DirectoryName + "/" + entry.FileName);
                        using var entryStream = file.Open();
                        using var streamWriter = new StreamWriter(entryStream);
                        streamWriter.Write(entry.Content);
                    }

                }
                memoryStream.Seek(0, SeekOrigin.Begin);
                await tablerService.SaveAsBinary($"{options.ClientName}.zip", "application/zip", memoryStream.ToArray());

            }
            catch (Exception ex)
            {
                var component = new RenderComponent<ExceptionModal>().Set(e => e.Exception, ex);
                var result = await modalService.ShowAsync("Error", component, new ModalOptions { Size = ModalSize.Large });
                             
               
            }
        }
    }

    public class GenerateOptions
    {
        public string Namespace { get; set; }
        public string ClientName { get; set; }
        public bool IncludeSubscriptions { get; set; }

        public string Url { get; set; }
        public string Token { get; set; }

        public string Schema { get; set; }
    }
}
