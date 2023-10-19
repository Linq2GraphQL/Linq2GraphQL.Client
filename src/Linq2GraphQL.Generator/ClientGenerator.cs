using Linq2GraphQL.Generator.Templates.Class;
using Linq2GraphQL.Generator.Templates.Client;
using Linq2GraphQL.Generator.Templates.Enum;
using Linq2GraphQL.Generator.Templates.Interface;
using Linq2GraphQL.Generator.Templates.Methods;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO.Compression;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Linq2GraphQL.Generator
{
    public class ClientGenerator : IDisposable
    {
        private readonly ZipArchive zipArchive;
        private readonly string namespaceName;
        private readonly string clientName;
        private readonly bool includeSubscriptions;
       // private readonly IConsole console;

        public ClientGenerator(ZipArchive zipArchive, string namespaceName, string clientName, bool includeSubscriptions)
        {

            this.zipArchive = zipArchive;
            this.namespaceName = namespaceName;
            this.clientName = clientName;
            this.includeSubscriptions = includeSubscriptions;
        }

        private void AddFile(string path, string fileName, string content)
        {
            var file = zipArchive.CreateEntry(path + "/" + fileName);

            using var entryStream = file.Open();
            using var streamWriter = new StreamWriter(entryStream);
            streamWriter.Write(content);
            
        }

        public async Task GenerateAsync(Uri uri, string authToken)
        {
            Console.WriteLine($"Start generating client for endpoint {uri.AbsoluteUri}");
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

            Console.WriteLine("Reading and deserializing schema information ...");
            var schemaJson = await response.Content.ReadAsStringAsync();

            Generate(schemaJson);
        }

        public void Generate(string schemaJson)
        {
            var rootSchema = JsonSerializer.Deserialize<RootSchema>(schemaJson,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            var schema = rootSchema.Data.Schema;
            var queryType = schema.QueryType;
            var mutationType = schema.MutationType;
            var subscriptionType = schema.SubscriptionType;


            var subscriptionsCount = schema.SubscriptionType?.AllFields?.Count;
            if (!includeSubscriptions && subscriptionsCount > 0)
            {
                Console.WriteLine("");
                Console.WriteLine(
                    "**************************************************************************************************************");
                Console.WriteLine(
                    $"Warning! Subscriptions is not included in client but schema has {subscriptionsCount} subscription(s) defined");
                Console.WriteLine("Set subscription flag to include in client.");
                Console.WriteLine(
                    "**************************************************************************************************************");
                Console.WriteLine("");
            }

            var includeSubs = includeSubscriptions && subscriptionsCount > 0;
            schema.PopulateFieldTypes();


            Console.WriteLine("Generate Interfaces");
            foreach (var classType in schema.GetInterfaces())
            {
                var classText = new InterfaceTemplate(classType, namespaceName).TransformText();
                AddFile("Interfaces", classType.FileName, classText);
            }



            Console.WriteLine("Generate Types...");
            foreach (var classType in schema.GetClassTypes().Where(e => e.Kind != TypeKind.InputObject && !e.IsPageInfo()))
            {
                var classText = new ClassTemplate(classType, namespaceName).TransformText();
                AddFile("Types", classType.FileName, classText);
            }

            var inputs = schema.GetClassTypes().Where(e => e.Kind == TypeKind.InputObject).ToList();
            foreach (var classType in inputs)
            {
                var classText = new InputClassTemplate(classType, namespaceName).TransformText();
                AddFile("Inputs", classType.FileName, classText);

            }

            //await GenerateInputFactory(namespaceName, inputs, outputPath);

            Console.WriteLine("Generate Enums...");
            //var enumDirectory = Path.Combine(outputPath, "Enums");
            // Directory.CreateDirectory(enumDirectory);
            foreach (var enumType in schema.GetEnums())
            {
                var enumText = new EnumTemplate(enumType, namespaceName).TransformText();
                AddFile("Enums", enumType.FileName, enumText);

                //var enumFilePath = Path.Combine(enumDirectory, enumType.Name.ToPascalCase() + ".cs");
                //await File.WriteAllTextAsync(enumFilePath, enumTemplate);
            }

            Console.WriteLine("Generate Methods...");
            var clientDirName = "Client";
            // var contextDirectory = Path.Combine(outputPath, "Client");
            //  Directory.CreateDirectory(contextDirectory);

            GenerateContextMethods(namespaceName, clientDirName, queryType, "OperationType.Query");
            GenerateContextMethods(namespaceName, clientDirName, mutationType, "OperationType.Mutation");

            if (includeSubscriptions)
            {
                 GenerateContextMethods(namespaceName, clientDirName, subscriptionType,
                    "OperationType.Subscription");
            }

            var includeQuery = queryType != null;
            var includeMutation = mutationType != null;

            Console.WriteLine("Generate Client...");
            var templateText = new ClientTemplate(namespaceName, clientName, includeQuery, includeMutation, includeSubscriptions).TransformText();
            var fileName = clientName + "Client" + ".cs";
            AddFile(clientDirName, fileName, templateText);
            //await File.WriteAllTextAsync(filePath, templateText);

            Console.WriteLine("Generate Client Extensions...");
            var clientExtensionsTemplateText =
                new ClientExtensionsTemplate(namespaceName, clientName, includeSubscriptions).TransformText();
            fileName= clientName + "ClientExtensions" + ".cs";
            AddFile(clientDirName, fileName, clientExtensionsTemplateText);
            //await File.WriteAllTextAsync(filePath, clientExtensionsTemplateText);
        }

        private static async Task GenerateInputFactory(string namespaceName, List<GraphqlType> inputs, string outputPath)
        {
            var inputFactoryTemplate = new InputFactoryClassTemplate(inputs, namespaceName).TransformText();
            var directory = Path.Combine(outputPath, "Input");
            Directory.CreateDirectory(directory);

            var classFilePath = Path.Combine(directory, "InputFactory.cs");
            await File.WriteAllTextAsync(classFilePath, inputFactoryTemplate);
        }

        private void GenerateContextMethods(string namespaceName, string directory, GraphqlType methodType,
            string schemaType)
        {
            if (methodType == null) { return; }

            var fileName = methodType.Name.ToPascalCase() + "Methods" + ".cs";
            var templateText = new MethodsTemplate(methodType, namespaceName, schemaType).TransformText();
            AddFile(directory, fileName, templateText);

            //  await File.WriteAllTextAsync(filePath, templateText);
        }





        public void Dispose()
        {
            //GC.SuppressFinalize(this);
            zipArchive?.Dispose();
        }
    }
}
