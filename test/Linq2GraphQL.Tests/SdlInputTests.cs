using System.Net.Http.Json;
using Linq2GraphQL.Generator;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;

namespace Linq2GraphQL.Tests;

/// <summary>
/// Generating from SDL must produce exactly the client that generating from the endpoint does, so the
/// two inputs are interchangeable. Each test server is read both ways and every generated file compared.
/// </summary>
public class SdlInputTests
{
    [Theory]
    [InlineData(false, true)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(true, false)]
    public async Task Sdl_GeneratesSameClientAsIntrospection(bool nullableServer, bool includeDeprecated)
    {
        using var application = nullableServer
            ? (IDisposable)new WebApplicationFactory<ProgramNullable>()
            : new WebApplicationFactory<Program>();
        using var httpClient = nullableServer
            ? ((WebApplicationFactory<ProgramNullable>)application).CreateClient()
            : ((WebApplicationFactory<Program>)application).CreateClient();

        var introspectionQuery = includeDeprecated ? General.IntrospectionQueryIncludeDeprecated : General.IntrospectionQuery;
        using var introspectionResponse = await httpClient.PostAsJsonAsync("/graphql/", new { query = introspectionQuery });
        introspectionResponse.EnsureSuccessStatusCode();
        var introspectionJson = await introspectionResponse.Content.ReadAsStringAsync();

        var sdl = await httpClient.GetStringAsync("/graphql/schema.graphql");

        var fromIntrospection = NewGenerator(nullableServer, includeDeprecated).Generate(introspectionJson);
        var fromSdl = NewGenerator(nullableServer, includeDeprecated).GenerateFromSdl(sdl);

        Files(fromSdl).Keys.ShouldBe(Files(fromIntrospection).Keys, ignoreOrder: true);
        foreach (var (path, content) in Files(fromIntrospection))
        {
            Files(fromSdl)[path].ShouldBe(content, $"{path} differs between SDL and introspection");
        }
    }

    [Fact]
    public void Sdl_WithoutQueryType_FailsWithClearMessage()
    {
        var ex = Should.Throw<GeneratorConfigurationException>(
            () => NewGenerator(false, false).GenerateFromSdl("type Thing { id: ID }"));

        ex.Message.ShouldContain("no query type");
    }

    [Fact]
    public void Sdl_ReferencingUnknownType_FailsWithClearMessage()
    {
        var ex = Should.Throw<GeneratorConfigurationException>(
            () => NewGenerator(false, false).GenerateFromSdl("type Query { thing: Thing }"));

        ex.Message.ShouldContain("Thing");
    }

    [Fact]
    public void Sdl_InvalidSyntax_FailsWithClearMessage()
    {
        var ex = Should.Throw<GeneratorConfigurationException>(
            () => NewGenerator(false, false).GenerateFromSdl("type Query {"));

        ex.Message.ShouldContain("not valid SDL");
    }

    [Fact]
    public void Sdl_SchemaBlockAndExtensions_AreApplied()
    {
        const string sdl = """
            schema { query: Root }
            type Root { thing: Thing }
            "A thing"
            type Thing { id: ID! }
            extend type Thing { name: String @deprecated(reason: "Use title") title: String }
            """;

        var withDeprecated = NewGenerator(false, true).GenerateFromSdl(sdl);
        var thing = withDeprecated.Single(e => e.FileName == "Thing.cs").Content;
        thing.ShouldContain("public string Title");
        thing.ShouldContain("[Obsolete(\"Use title\")]");
        withDeprecated.ShouldContain(e => e.FileName == "RootMethods.cs");

        var withoutDeprecated = NewGenerator(false, false).GenerateFromSdl(sdl);
        withoutDeprecated.Single(e => e.FileName == "Thing.cs").Content.ShouldNotContain("public string Name");
    }

    private static ClientGenerator NewGenerator(bool nullable, bool includeDeprecated) =>
        new("TestNs", "TestClient", true, EnumGeneratorStrategy.FailIfMissing, nullable, includeDeprecated);

    private static Dictionary<string, string> Files(List<FileEntry> entries) =>
        entries.ToDictionary(e => $"{e.DirectoryName}/{e.FileName}", e => e.Content);
}
