using System.CommandLine;
using System.CommandLine.Parsing;

namespace Linq2GraphQL.Generator;

internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        var uriArgument = new Argument<Uri>("endpoint", "Endpoint of the GraphQL service")
        {
            // Optional, because a config file may supply it instead.
            Arity = ArgumentArity.ZeroOrOne
        };

        var configFile = new Option<string>(new[] { "--config", "-cf" },
            $"Json settings file, defaults to {GeneratorConfig.DefaultFileName} in the current directory. Every " +
            "setting in it is optional and is overridden by an explicitly passed option");
        var outputFolder = new Option<string>(new[] { "--output", "-o" }, "Output folder, relative to current location");
        var namespaceName = new Option<string>(new[] { "--namespace", "-n" }, "Namespace of generated classes");
        var clientName = new Option<string>(new[] { "--client", "-c" }, "Name of the generated client");
        var authToken = new Option<string>(new[] { "--token", "-t" }, "Bearertoken for authentication");
        var includeSubscriptions = new Option<bool>(new[] { "--subscriptions", "-s" }, "Include subscriptions");
        var enumStrategy = new Option<string>(new[] { "--enum-strategy", "-es" }, "Enum strategy");
        var nullable = new Option<bool>(new[] { "--nullable", "-nu" }, "Nullable client");
        var includeDeprecated = new Option<bool>(new[] { "--deprecated", "-d" }, "Include Deprecated as Obsolete");

        var rootCommand = new RootCommand("Generate GraphQL client")
        {
            uriArgument,
            configFile,
            outputFolder,
            namespaceName,
            clientName,
            authToken,
            includeSubscriptions,
            enumStrategy,
            nullable,
            includeDeprecated
        };

        rootCommand.SetHandler(async context =>
            {
                var result = context.ParseResult;

                try
                {
                    var configPath = result.GetValueForOption(configFile);
                    var config = GeneratorConfig.Resolve(configPath);

                    // Fail before touching the network if a mapping target is bad.
                    var scalarMappings = Helpers.ResolveScalarMappings(config.ScalarMappings);

                    var uriValue = ResolveEndpoint(result, uriArgument, config.Endpoint);
                    var outputFolderValue = Resolve(result, outputFolder, config.Output, "Linq2GraphQL_Generated");
                    var namespaceValue = Resolve(result, namespaceName, config.Namespace, "YourNamespace");
                    var clientNameValue = Resolve(result, clientName, config.Client, "GraphQLClient");
                    var authTokenValue = Resolve(result, authToken, config.Token, null);
                    var enumStrategyValue = Resolve(result, enumStrategy, config.EnumStrategy, null);
                    var includeSubscriptionsValue = Resolve(result, includeSubscriptions, config.Subscriptions, false);
                    var nullableValue = Resolve(result, nullable, config.Nullable, false);
                    var deprecatedValue = Resolve(result, includeDeprecated, config.Deprecated, false);

                    await GenerateClientAsync(uriValue, outputFolderValue, namespaceValue, clientNameValue,
                        includeSubscriptionsValue, authTokenValue, enumStrategyValue, nullableValue, deprecatedValue,
                        scalarMappings);
                }
                catch (GeneratorConfigurationException ex)
                {
                    Console.Error.WriteLine(ex.Message);
                    context.ExitCode = 1;
                }
            }
        );

        return await rootCommand.InvokeAsync(args);
    }

    /// <summary>
    /// An explicitly passed option always wins over the config file, which in turn wins over the
    /// built-in default. Options deliberately carry no default value factory so that
    /// <see cref="ParseResult.FindResultFor(Option)"/> can tell "not passed" from "passed the default".
    /// </summary>
    private static T Resolve<T>(ParseResult result, Option<T> option, T configValue, T defaultValue)
        where T : class
    {
        if (result.FindResultFor(option) != null)
        {
            return result.GetValueForOption(option);
        }

        return configValue ?? defaultValue;
    }

    private static T Resolve<T>(ParseResult result, Option<T> option, T? configValue, T defaultValue)
        where T : struct
    {
        if (result.FindResultFor(option) != null)
        {
            return result.GetValueForOption(option);
        }

        return configValue ?? defaultValue;
    }

    private static Uri ResolveEndpoint(ParseResult result, Argument<Uri> argument, string configEndpoint)
    {
        var uriValue = result.GetValueForArgument(argument);
        if (uriValue != null)
        {
            return uriValue;
        }

        if (string.IsNullOrWhiteSpace(configEndpoint))
        {
            throw new GeneratorConfigurationException(
                "No endpoint given. Pass it as the first argument or set \"endpoint\" in the config file.");
        }

        if (!Uri.TryCreate(configEndpoint, UriKind.Absolute, out var configUri))
        {
            throw new GeneratorConfigurationException($"\"endpoint\" is not a valid absolute uri: {configEndpoint}");
        }

        return configUri;
    }

    private static async Task GenerateClientAsync(Uri uri, string outputFolder, string namespaceName, string name,
        bool includeSubscriptions, string authToken, string enumStrategy, bool nullable, bool includeDeprecated,
        IReadOnlyDictionary<string, (string Name, Type type)?> scalarMappings)
    {
        var enumStrat = enumStrategy != null &&
                        enumStrategy.Equals("AddUnknownOption", StringComparison.InvariantCultureIgnoreCase)
            ? EnumGeneratorStrategy.AddUnknownOption
            : EnumGeneratorStrategy.FailIfMissing;

        var generator = new ClientGenerator(namespaceName, name, includeSubscriptions, enumStrat, nullable,
            includeDeprecated, scalarMappings);
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
                await File.WriteAllTextAsync(filePath, entry.Content.ReplaceLineEndings("\n"));
            }
        }
    }
}
