using Microsoft.AspNetCore.Components;
using StarWars.Client;
using System.CommandLine;
using System.CommandLine.IO;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using TabBlazor.Services;

namespace Linq2GraphQL.Docs.Components
{


    public partial class GenerateClient
    {
        [Inject] private TablerService tablerService { get; set; }

        private GenerateOptions options = new();

        protected override void OnInitialized()
        {
            options.Namespace = "StarWars.Client";
            options.ClientName = "StarWars";

            options.Url = "https://swapi-graphql.netlify.app/.netlify/functions/index";
        }

        private async Task GenerateClientAsync()
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
            var bytes = memoryStream.ToArray();
            await tablerService.SaveAsBinary($"{options.ClientName}.zip", "application/zip", bytes);

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
