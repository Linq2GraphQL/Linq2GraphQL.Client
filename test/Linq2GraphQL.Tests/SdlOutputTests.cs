using System.Net.Http.Json;
using GraphQLParser;
using Linq2GraphQL.Client;
using Linq2GraphQL.Generator;
using Linq2GraphQL.TestClient;
using Linq2GraphQL.TestClientNullable;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;

namespace Linq2GraphQL.Tests;

/// <summary>
/// Covers agent mode in the generator: the SDL constant, IGraphSchemaSource on the client, and that the
/// emitted SDL describes exactly the schema the client was generated from.
/// </summary>
[Collection(GeneratorCollection.Name)]
public class SdlOutputTests
{
    [Fact]
    public void CheckedInClients_ImplementGraphSchemaSource()
    {
        var sdl = SdlOf<SampleClient>();

        sdl.ShouldBe(SampleClientSchema.Sdl);
        sdl.ShouldContain("type Query {");
        SdlOf<SampleNullableClient>().ShouldBe(SampleNullableClientSchema.Sdl);
    }

    [Fact]
    public void CheckedInSdl_IsValidSdl()
    {
        Should.NotThrow(() => Parser.Parse(SampleClientSchema.Sdl));
        Should.NotThrow(() => Parser.Parse(SampleNullableClientSchema.Sdl));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EmittedSdl_RoundTripsToTheSameClient(bool nullableServer)
    {
        var introspectionJson = await IntrospectAsync(nullableServer);

        var fromIntrospection = NewGenerator(nullableServer, agent: true).Generate(introspectionJson);
        var sdl = Sdl(fromIntrospection);
        var fromSdl = NewGenerator(nullableServer, agent: true).GenerateFromSdl(sdl);

        Files(fromSdl).Keys.ShouldBe(Files(fromIntrospection).Keys, ignoreOrder: true);
        foreach (var (path, content) in Files(fromIntrospection))
        {
            Files(fromSdl)[path].ShouldBe(content, $"{path} differs after an SDL round trip");
        }
    }

    [Fact]
    public async Task WithoutAgent_NoSchemaFileAndNoInterface()
    {
        var entries = NewGenerator(false, agent: false).Generate(await IntrospectAsync(false));

        entries.ShouldNotContain(e => e.FileName == "TestClientSchema.cs");
        entries.Single(e => e.FileName == "TestClient.cs").Content.ShouldNotContain("IGraphSchemaSource");
    }

    [Fact]
    public void DescriptionWithQuotes_SurvivesTheRawStringAndRoundTrips()
    {
        const string sdl = "type Query {\n  \"\"\"\n  Text with \\\"\"\" and \"\" inside\n  \"\"\"\n  thing: String\n}\n";

        var schemaFile = NewGenerator(false, agent: true).GenerateFromSdl(sdl)
            .Single(e => e.FileName == "TestClientSchema.cs").Content;
        var emitted = Sdl(schemaFile);

        var longestRun = System.Text.RegularExpressions.Regex.Matches(emitted, "\"+").Max(e => e.Length);
        schemaFile.ShouldContain("Sdl = " + new string('"', Math.Max(3, longestRun + 1)) + "\n");

        var methods = NewGenerator(false, agent: true).GenerateFromSdl(emitted)
            .Single(e => e.FileName == "QueryMethods.cs").Content;
        methods.ShouldContain("Description(\"Text with \\\"\\\"\\\" and \\\"\\\" inside\")");
    }

    private static string SdlOf<T>() where T : IGraphSchemaSource => T.Sdl;

    private static async Task<string> IntrospectAsync(bool nullableServer)
    {
        using var application = nullableServer
            ? (IDisposable)new WebApplicationFactory<ProgramNullable>()
            : new WebApplicationFactory<Program>();
        using var httpClient = nullableServer
            ? ((WebApplicationFactory<ProgramNullable>)application).CreateClient()
            : ((WebApplicationFactory<Program>)application).CreateClient();

        using var response = await httpClient.PostAsJsonAsync("/graphql/",
            new { query = General.IntrospectionQueryIncludeDeprecated });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    private static ClientGenerator NewGenerator(bool nullable, bool agent) =>
        new("TestNs", "TestClient", true, EnumGeneratorStrategy.FailIfMissing, nullable, true, agent: agent);

    private static string Sdl(List<FileEntry> entries) =>
        Sdl(entries.Single(e => e.FileName == "TestClientSchema.cs").Content);

    /// <summary>
    /// The SDL inside a generated schema file: the raw string literal's lines, with its indentation removed.
    /// </summary>
    private static string Sdl(string schemaFile)
    {
        var lines = schemaFile.Split('\n');
        var start = Array.FindIndex(lines, e => e.Contains("Sdl = \"")) + 1;
        var end = Array.FindLastIndex(lines, e => e.TrimStart().StartsWith("\"\"\"") && e.TrimEnd().EndsWith(";"));
        return string.Join("\n", lines[start..end].Select(e => e.Length >= 8 ? e[8..] : e.TrimStart())) + "\n";
    }

    private static Dictionary<string, string> Files(List<FileEntry> entries) =>
        entries.ToDictionary(e => $"{e.DirectoryName}/{e.FileName}", e => e.Content);
}
