using Linq2GraphQL.StarWars;
using Microsoft.AspNetCore.Components;
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
        [Inject] private StarWarsClient starWarsClient { get; set; }


        protected override async  Task OnInitializedAsync()
        {

            var films = await starWarsClient
                .Query
                .AllFilms()
                .Select(e=> e.Films)
                .ExecuteAsync();


            await base.OnInitializedAsync();
        }

        private async Task GenerateClientAsync()
        {

            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                var generator = new Generator.ClientGenerator(archive, "Linq2GraphQL.StarWars", "StarWars", false);
                await generator.GenerateAsync(new Uri("https://swapi-graphql.netlify.app/.netlify/functions/index"), null);
            }
            memoryStream.Seek(0, SeekOrigin.Begin);
            var bytes = memoryStream.ToArray();
            await tablerService.SaveAsBinary("test.zip", "application/zip", bytes);


            //    using var memoryStream = new MemoryStream();
            //    using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            //    {
            //        var demoFile = archive.CreateEntry("jocke/foo.txt");

            //        using var entryStream = demoFile.Open();
            //        using var streamWriter = new StreamWriter(entryStream);
            //        streamWriter.Write("Bar!");
            //    }

            //    memoryStream.Seek(0, SeekOrigin.Begin);
            //    var bytes = memoryStream.ToArray();
            //    await tablerService.SaveAsBinary("test.zip", "application/zip", bytes);

            //}
            //}
        }
    }
}
