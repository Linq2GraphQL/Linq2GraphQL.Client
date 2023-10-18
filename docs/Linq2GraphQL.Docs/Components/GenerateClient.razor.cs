using Microsoft.AspNetCore.Components;
using System.IO.Compression;
using TabBlazor.Services;

namespace Linq2GraphQL.Docs.Components
{
    public partial class GenerateClient
    {
        [Inject] private TablerService tablerService { get; set; }

        private async Task GenerateClientAsync()
        {
            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                var demoFile = archive.CreateEntry("foo.txt");

                using (var entryStream = demoFile.Open())
                using (var streamWriter = new StreamWriter(entryStream))
                {
                    streamWriter.Write("Bar!");
                }
            }

            memoryStream.Seek(0, SeekOrigin.Begin);
            var bytes = memoryStream.ToArray();
            await tablerService.SaveAsBinary("test.zip", "application/zip", bytes);
         
        }
    }
}