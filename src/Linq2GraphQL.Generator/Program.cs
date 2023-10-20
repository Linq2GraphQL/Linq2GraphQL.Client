using System.CommandLine;

namespace Linq2GraphQL.Generator;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var uriArgument = new Argument<Uri>("endpoint", "Endpoint of the GraphQL service")
        {
            Arity = ArgumentArity.ExactlyOne
        };

        var outputFolder = new Option<string>(new[] { "--output", "-o" }, () => "Linq2GraphQL_Generated",
            "Output folder, relative to current location");
        var namespaceName = new Option<string>(new[] { "--namespace", "-n" }, () => "YourNamespace",
            "Namespace of generated classes");
        var clientName = new Option<string>(new[] { "--client", "-c" }, () => "GraphQLClient",
            "Name of the generated client");
        var authToken = new Option<string>(new[] { "--token", "-t" }, "Bearertoken for authentication");
        var includeSubscriptions = new Option<bool>(new[] { "--subscriptions", "-s" }, "Include subscriptions");

        var rootCommand = new RootCommand("Generate GraphQL client")
        {
            uriArgument,
            outputFolder,
            namespaceName,
            clientName,
            authToken,
            includeSubscriptions
        };

        rootCommand.SetHandler(async context =>
            {
                var result = context.ParseResult;
                var uriValue = result.GetValueForArgument(uriArgument);
                var outputFolderValue = result.GetValueForOption(outputFolder);
                var namespaceValue = result.GetValueForOption(namespaceName);
                var clientNameValue = result.GetValueForOption(clientName);
                var authTokenValue = result.GetValueForOption(authToken);
                var includeSubscriptionsValue = result.GetValueForOption(includeSubscriptions);

                await GenerateClientAsync(uriValue, outputFolderValue, namespaceValue, clientNameValue,
                    includeSubscriptionsValue, authTokenValue);
            }
        );

            await rootCommand.InvokeAsync(args);
      

    }

    private static async Task GenerateClientAsync(Uri uri, string outputFolder, string namespaceName, string name,
        bool includeSubscriptions, string authToken)
    {
        var generator = new ClientGenerator(namespaceName, name, includeSubscriptions);
        var entries = await generator.GenerateAsync(uri, authToken);

        var outputPath = Path.GetFullPath(outputFolder, Environment.CurrentDirectory);
        Console.WriteLine($"Output path is set to: {outputPath}");
        foreach (var dirName in entries.GroupBy(e => e.DirectoryName))
        {
            var directory = Path.Combine(outputPath, dirName.Key);
            Directory.CreateDirectory(directory);

            foreach (var entry in dirName)
            {
                var filePath = Path.Combine(directory, entry.FileName);
                await File.WriteAllTextAsync(filePath, entry.Content);
            }

        }
    }
}