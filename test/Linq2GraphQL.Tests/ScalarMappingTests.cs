using System.Text.Json;
using Linq2GraphQL.Generator;
using Shouldly;

namespace Linq2GraphQL.Tests;

/// <summary>
/// Covers the scalarMappings config: overriding how a GraphQL scalar maps to a CLR type, and the
/// knock-on effect that a mapped scalar no longer gets a CustomScalar class generated for it.
/// </summary>
public class ScalarMappingTests
{
    /// <summary>
    /// A hand-written introspection response with three scalars: one mapped by default
    /// (DateTime -> DateTimeOffset) and two that are not (BigInt, Json).
    /// </summary>
    private const string SchemaJson = """
    {
      "data": {
        "__schema": {
          "queryType": { "name": "Query" },
          "types": [
            {
              "kind": "OBJECT",
              "name": "Query",
              "fields": [
                { "name": "thing", "args": [], "type": { "kind": "OBJECT", "name": "Thing", "ofType": null } }
              ]
            },
            {
              "kind": "OBJECT",
              "name": "Thing",
              "fields": [
                { "name": "id", "args": [], "type": { "kind": "SCALAR", "name": "BigInt", "ofType": null } },
                { "name": "when", "args": [], "type": { "kind": "SCALAR", "name": "DateTime", "ofType": null } },
                { "name": "payload", "args": [], "type": { "kind": "SCALAR", "name": "Json", "ofType": null } }
              ]
            },
            { "kind": "SCALAR", "name": "BigInt" },
            { "kind": "SCALAR", "name": "DateTime" },
            { "kind": "SCALAR", "name": "Json" },
            { "kind": "SCALAR", "name": "String" }
          ]
        }
      }
    }
    """;

    private static List<FileEntry> Generate(Dictionary<string, string>? scalarMappings = null)
    {
        var resolved = Helpers.ResolveScalarMappings(scalarMappings);
        var generator = new ClientGenerator("TestNs", "TestClient", false, EnumGeneratorStrategy.FailIfMissing,
            false, false, resolved);
        return generator.Generate(SchemaJson);
    }

    private static string ThingType(List<FileEntry> entries)
    {
        return entries.Single(e => e.DirectoryName == "Types" && e.FileName == "Thing.cs").Content;
    }

    private static IEnumerable<string> ScalarFiles(List<FileEntry> entries)
    {
        return entries.Where(e => e.DirectoryName == "Scalars").Select(e => e.FileName);
    }

    [Fact]
    public void NoOverrides_UsesBuiltInMapping()
    {
        var entries = Generate();

        ThingType(entries).ShouldContain("DateTimeOffset? When");
        ScalarFiles(entries).ShouldBe(new[] { "BigInt.cs", "Json.cs" }, ignoreOrder: true);
    }

    [Fact]
    public void Override_ChangesAnAlreadyMappedScalar()
    {
        var entries = Generate(new Dictionary<string, string> { ["DateTime"] = "System.DateTime" });

        ThingType(entries).ShouldContain("DateTime? When");
        ThingType(entries).ShouldNotContain("DateTimeOffset");
    }

    [Fact]
    public void Override_MapsACustomScalarToASimpleType_AndStopsGeneratingItsClass()
    {
        var entries = Generate(new Dictionary<string, string> { ["BigInt"] = "long" });

        ThingType(entries).ShouldContain("long? Id");
        ScalarFiles(entries).ShouldBe(new[] { "Json.cs" });
    }

    [Fact]
    public void Override_ToNull_OptsAScalarOutAndGeneratesACustomScalarInstead()
    {
        var entries = Generate(new Dictionary<string, string> { ["DateTime"] = null! });

        ThingType(entries).ShouldContain("DateTime When");
        ThingType(entries).ShouldNotContain("DateTimeOffset");
        ScalarFiles(entries).ShouldBe(new[] { "BigInt.cs", "DateTime.cs", "Json.cs" }, ignoreOrder: true);
    }

    [Theory]
    [InlineData("long")]
    [InlineData("Int64")]
    [InlineData("System.Int64")]
    [InlineData("LONG")]
    public void TargetType_AcceptsEverySupportedSpelling(string targetType)
    {
        var entries = Generate(new Dictionary<string, string> { ["BigInt"] = targetType });

        ThingType(entries).ShouldContain("long? Id");
    }

    [Fact]
    public void ScalarName_IsMatchedCaseInsensitively()
    {
        var entries = Generate(new Dictionary<string, string> { ["bigint"] = "long" });

        ThingType(entries).ShouldContain("long? Id");
        ScalarFiles(entries).ShouldBe(new[] { "Json.cs" });
    }

    [Fact]
    public void UnsupportedTargetType_FailsWithAHelpfulMessage()
    {
        var ex = Should.Throw<GeneratorConfigurationException>(() =>
            Helpers.ResolveScalarMappings(new Dictionary<string, string> { ["Money"] = "My.App.Money" }));

        ex.Message.ShouldContain("My.App.Money");
        ex.Message.ShouldContain("Money");
        ex.Message.ShouldContain("DateTimeOffset");
    }

    [Fact]
    public void Overrides_DoNotLeakBetweenRuns()
    {
        Generate(new Dictionary<string, string> { ["BigInt"] = "long" });

        var entries = Generate();

        ThingType(entries).ShouldContain("BigInt Id");
        ScalarFiles(entries).ShouldBe(new[] { "BigInt.cs", "Json.cs" }, ignoreOrder: true);
        Helpers.DefaultTypeMapping.ContainsKey("BigInt").ShouldBeFalse();
    }

    [Fact]
    public void ConfigFile_RoundTripsEverySetting()
    {
        var json = """
        {
          "endpoint": "https://example.com/graphql",
          "output": "Generated",
          "namespace": "My.Ns",
          "client": "MyClient",
          "nullable": true,
          "subscriptions": true,
          "scalarMappings": {
            "DateTime": "System.DateTime",
            "BigInt": "long",
            "Json": null
          }
        }
        """;

        var config = JsonSerializer.Deserialize<GeneratorConfig>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        config.ShouldNotBeNull();
        config.Endpoint.ShouldBe("https://example.com/graphql");
        config.Namespace.ShouldBe("My.Ns");
        config.Client.ShouldBe("MyClient");
        config.Nullable.ShouldBe(true);
        config.Subscriptions.ShouldBe(true);
        config.Deprecated.ShouldBeNull();

        var resolved = Helpers.ResolveScalarMappings(config.ScalarMappings);
        resolved["DateTime"]!.Value.Name.ShouldBe("DateTime");
        resolved["BigInt"]!.Value.Name.ShouldBe("long");
        resolved["Json"].ShouldBeNull();
    }

    [Fact]
    public void ConfigFile_MissingFile_FailsWithAHelpfulMessage()
    {
        var missing = Path.Combine(Path.GetTempPath(), $"linq2graphql-missing-{Guid.NewGuid():N}.json");

        var ex = Should.Throw<GeneratorConfigurationException>(() => GeneratorConfig.Load(missing));

        ex.Message.ShouldContain("not found");
    }

    [Fact]
    public void ConfigFile_InvalidJson_FailsWithAHelpfulMessage()
    {
        var path = Path.Combine(Path.GetTempPath(), $"linq2graphql-bad-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, "{ not json");

        try
        {
            var ex = Should.Throw<GeneratorConfigurationException>(() => GeneratorConfig.Load(path));
            ex.Message.ShouldContain("not valid JSON");
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string NewTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"linq2graphql-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }

    [Fact]
    public void Resolve_PicksUpTheDefaultFileInTheWorkingDirectory()
    {
        var directory = NewTempDirectory();
        try
        {
            File.WriteAllText(Path.Combine(directory, GeneratorConfig.DefaultFileName),
                """{ "client": "FromDefaultFile" }""");

            GeneratorConfig.Resolve(null, directory).Client.ShouldBe("FromDefaultFile");
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void Resolve_WithoutAnyFile_ReturnsAnEmptyConfig()
    {
        var directory = NewTempDirectory();
        try
        {
            var config = GeneratorConfig.Resolve(null, directory);

            config.Client.ShouldBeNull();
            config.Endpoint.ShouldBeNull();
            config.ScalarMappings.ShouldBeNull();
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void Resolve_ExplicitPathWinsOverTheDefaultFile()
    {
        var directory = NewTempDirectory();
        try
        {
            File.WriteAllText(Path.Combine(directory, GeneratorConfig.DefaultFileName),
                """{ "client": "FromDefaultFile" }""");

            var explicitPath = Path.Combine(directory, "other.json");
            File.WriteAllText(explicitPath, """{ "client": "FromExplicitFile" }""");

            GeneratorConfig.Resolve(explicitPath, directory).Client.ShouldBe("FromExplicitFile");
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void Resolve_ExplicitPathThatDoesNotExist_StillFails()
    {
        var directory = NewTempDirectory();
        try
        {
            // The default file being present must not paper over a bad --config.
            File.WriteAllText(Path.Combine(directory, GeneratorConfig.DefaultFileName),
                """{ "client": "FromDefaultFile" }""");

            Should.Throw<GeneratorConfigurationException>(
                () => GeneratorConfig.Resolve(Path.Combine(directory, "missing.json"), directory));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static GeneratorConfig LoadJson(string json)
    {
        var directory = NewTempDirectory();
        try
        {
            var path = Path.Combine(directory, GeneratorConfig.DefaultFileName);
            File.WriteAllText(path, json);
            return GeneratorConfig.Load(path);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void Load_UnknownSetting_FailsInsteadOfBeingIgnored()
    {
        // The singular "scalarMapping" is the typo this is really guarding against: System.Text.Json
        // would drop it and generate a client with none of the mappings applied.
        var ex = Should.Throw<GeneratorConfigurationException>(
            () => LoadJson("""{ "scalarMapping": { "BigInt": "long" } }"""));

        ex.Message.ShouldContain("scalarMapping");
        ex.Message.ShouldContain("scalarMappings");
        ex.Message.ShouldContain("enumStrategy");
    }

    [Fact]
    public void Load_SeveralUnknownSettings_AreAllReported()
    {
        var ex = Should.Throw<GeneratorConfigurationException>(
            () => LoadJson("""{ "nope": 1, "alsoNope": 2 }"""));

        ex.Message.ShouldContain("nope");
        ex.Message.ShouldContain("alsoNope");
        ex.Message.ShouldContain("Unknown settings");
    }

    [Fact]
    public void Load_AcceptsEverySettingTheCommandLineHas()
    {
        var config = LoadJson("""
        {
          "endpoint": "https://example.com/graphql",
          "output": "Generated",
          "namespace": "My.Ns",
          "client": "MyClient",
          "token": "secret",
          "subscriptions": true,
          "enumStrategy": "AddUnknownOption",
          "nullable": true,
          "deprecated": true,
          "scalarMappings": { "BigInt": "long" }
        }
        """);

        config.Endpoint.ShouldBe("https://example.com/graphql");
        config.Output.ShouldBe("Generated");
        config.Namespace.ShouldBe("My.Ns");
        config.Client.ShouldBe("MyClient");
        config.Token.ShouldBe("secret");
        config.Subscriptions.ShouldBe(true);
        config.EnumStrategy.ShouldBe("AddUnknownOption");
        config.Nullable.ShouldBe(true);
        config.Deprecated.ShouldBe(true);
        config.ScalarMappings!["BigInt"].ShouldBe("long");
    }

    [Fact]
    public void Load_SettingNamesAreCaseInsensitive()
    {
        var config = LoadJson("""{ "Endpoint": "https://example.com/graphql", "NULLABLE": true }""");

        config.Endpoint.ShouldBe("https://example.com/graphql");
        config.Nullable.ShouldBe(true);
    }

    [Fact]
    public void Load_RootThatIsNotAnObject_FailsWithAHelpfulMessage()
    {
        var ex = Should.Throw<GeneratorConfigurationException>(() => LoadJson("[]"));

        ex.Message.ShouldContain("must contain a json object");
    }
}
