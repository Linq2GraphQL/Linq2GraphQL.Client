using System.CommandLine;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Linq2GraphQL.Generator.Templates.Class;
using Linq2GraphQL.Generator.Templates.Client;
using Linq2GraphQL.Generator.Templates.Enum;
using Linq2GraphQL.Generator.Templates.Interface;
using Linq2GraphQL.Generator.Templates.Methods;

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

                await GenerateClient(uriValue, outputFolderValue, namespaceValue, clientNameValue,
                    includeSubscriptionsValue, authTokenValue, context.Console);
            }
        );

        try
        {
            await rootCommand.InvokeAsync(args);
        }
        catch (Exception ex)
        {
            var tt = ex.Message;
            throw;
        }


    }

    private static async Task GenerateClient(Uri uri, string outputFolder, string namespaceName, string name,
        bool includeSubscriptions, string authToken, IConsole console)
    {
        console.WriteLine($"Start generating client for endpoint {uri.AbsoluteUri}");

        using var httpClient = new HttpClient();

        if (!string.IsNullOrWhiteSpace(authToken))
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
        }

        using var response = await httpClient.PostAsJsonAsync(uri, new { query = General.IntrospectionQuery });

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(
                $"Failed to get schema. Error: {response.StatusCode}. Details: {await response.Content?.ReadAsStringAsync()}");
        }

        console.WriteLine("Reading and deserializing schema information ...");
        var schemaJson = await response.Content.ReadAsStringAsync();
        var rootSchema = JsonSerializer.Deserialize<RootSchema>(schemaJson,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        var schema = rootSchema.Data.Schema;
        var queryType = schema.QueryType;
        var mutationType = schema.MutationType;
        var subscriptionType = schema.SubscriptionType;

        var outputPath = Path.GetFullPath(outputFolder, Environment.CurrentDirectory);
        console.WriteLine($"Output path is set to: {outputPath}");

        var subscriptionsCount = schema.SubscriptionType?.AllFields?.Count;
        if (!includeSubscriptions && subscriptionsCount > 0)
        {
            console.WriteLine("");
            console.WriteLine(
                "**************************************************************************************************************");
            console.WriteLine(
                $"Warning! Subscriptions is not included in client but schema has {subscriptionsCount} subscription(s) defined");
            console.WriteLine("Set subscription flag to include in client.");
            console.WriteLine(
                "**************************************************************************************************************");
            console.WriteLine("");
        }

        includeSubscriptions = includeSubscriptions && subscriptionsCount > 0;
        schema.PopulateFieldTypes();


        Directory.CreateDirectory(outputPath);

        console.WriteLine("Generate Interfaces");
        foreach (var classType in schema.GetInterfaces())
        {
            var classTemplate = new InterfaceTemplate(classType, namespaceName).TransformText();
            var directory = Path.Combine(outputPath, "Interfaces");
            Directory.CreateDirectory(directory);

            var classFilePath = Path.Combine(directory, classType.Name.ToPascalCase() + ".cs");
            await File.WriteAllTextAsync(classFilePath, classTemplate);
        }



        console.WriteLine("Generate Classes...");
        foreach (var classType in schema.GetClassTypes().Where(e => e.Kind != TypeKind.InputObject && !e.IsPageInfo()))
        {
            var classTemplate = new ClassTemplate(classType, namespaceName).TransformText();
            var directory = Path.Combine(outputPath, "Types");
            Directory.CreateDirectory(directory);

            var classFilePath = Path.Combine(directory, classType.Name.ToPascalCase() + ".cs");
            await File.WriteAllTextAsync(classFilePath, classTemplate);
        }

        var inputs = schema.GetClassTypes().Where(e => e.Kind == TypeKind.InputObject).ToList();
        foreach (var classType in inputs)
        {
            var classTemplate = new InputClassTemplate(classType, namespaceName).TransformText();
            var directory = Path.Combine(outputPath, "Input");
            Directory.CreateDirectory(directory);

            var classFilePath = Path.Combine(directory, classType.Name.ToPascalCase() + ".cs");
            await File.WriteAllTextAsync(classFilePath, classTemplate);
        }

        await GenerateInputFactory(namespaceName, inputs, outputPath);

        console.WriteLine("Generate Enums...");
        var enumDirectory = Path.Combine(outputPath, "Enums");
        Directory.CreateDirectory(enumDirectory);
        foreach (var enumType in schema.GetEnums())
        {
            var enumTemplate = new EnumTemplate(enumType, namespaceName).TransformText();
            var enumFilePath = Path.Combine(enumDirectory, enumType.Name.ToPascalCase() + ".cs");
            await File.WriteAllTextAsync(enumFilePath, enumTemplate);
        }

        console.WriteLine("Generate Methods...");
        var contextDirectory = Path.Combine(outputPath, "Client");
        Directory.CreateDirectory(contextDirectory);

        await GenerateContextMethods(namespaceName, contextDirectory, queryType, "OperationType.Query");
        await GenerateContextMethods(namespaceName, contextDirectory, mutationType, "OperationType.Mutation");

        if (includeSubscriptions)
        {
            await GenerateContextMethods(namespaceName, contextDirectory, subscriptionType,
                "OperationType.Subscription");
        }

        var includeQuery = queryType != null;
        var includeMutation = mutationType != null;

        console.WriteLine("Generate Client...");
        var templateText = new ClientTemplate(namespaceName, name, includeQuery, includeMutation, includeSubscriptions).TransformText();
        var filePath = Path.Combine(contextDirectory, name + "Client" + ".cs");
        await File.WriteAllTextAsync(filePath, templateText);

        console.WriteLine("Generate Client Extensions...");
        var clientExtensionsTemplateText =
            new ClientExtensionsTemplate(namespaceName, name, includeSubscriptions).TransformText();
        filePath = Path.Combine(contextDirectory, name + "ClientExtensions" + ".cs");
        await File.WriteAllTextAsync(filePath, clientExtensionsTemplateText);
    }

    private static async Task GenerateInputFactory(string namespaceName, List<GraphqlType> inputs, string outputPath)
    {
        var inputFactoryTemplate = new InputFactoryClassTemplate(inputs, namespaceName).TransformText();
        var directory = Path.Combine(outputPath, "Input");
        Directory.CreateDirectory(directory);

        var classFilePath = Path.Combine(directory, "InputFactory.cs");
        await File.WriteAllTextAsync(classFilePath, inputFactoryTemplate);
    }

    private static async Task GenerateContextMethods(string namespaceName, string directory, GraphqlType methodType,
        string schemaType)
    {
        if (methodType == null) { return; }

        var filePath = Path.Combine(directory, methodType.Name.ToPascalCase() + "Methods" + ".cs");
        var templateText = new MethodsTemplate(methodType, namespaceName, schemaType).TransformText();
        await File.WriteAllTextAsync(filePath, templateText);
    }
}