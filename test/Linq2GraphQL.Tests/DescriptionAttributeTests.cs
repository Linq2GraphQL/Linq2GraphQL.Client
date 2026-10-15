using Linq2GraphQL.Generator;
using Shouldly;

namespace Linq2GraphQL.Tests;

/// <summary>
/// Covers the [Description] attributes the generator emits from schema descriptions, their escaping,
/// the descriptionAttributes opt-out, and that the extra introspection fields (possibleTypes,
/// defaultValue) deserialize.
/// </summary>
public class DescriptionAttributeTests
{
    private const string Attribute = "[global::System.ComponentModel.Description(";

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
                {
                  "name": "thing",
                  "description": "Get one thing",
                  "args": [
                    { "name": "id", "description": "The \"id\" of the thing", "defaultValue": "\"1\"",
                      "type": { "kind": "SCALAR", "name": "String", "ofType": null } }
                  ],
                  "type": { "kind": "OBJECT", "name": "Thing", "ofType": null }
                }
              ]
            },
            {
              "kind": "OBJECT",
              "name": "Thing",
              "description": "A thing.\nSecond line with a back\\slash.",
              "fields": [
                { "name": "name", "description": "The name", "args": [],
                  "type": { "kind": "SCALAR", "name": "String", "ofType": null } },
                { "name": "status", "args": [], "type": { "kind": "ENUM", "name": "Status", "ofType": null } }
              ]
            },
            {
              "kind": "ENUM",
              "name": "Status",
              "description": "Thing status",
              "enumValues": [
                { "name": "ACTIVE", "description": "Currently in use" },
                { "name": "CLOSED" }
              ]
            },
            {
              "kind": "INPUT_OBJECT",
              "name": "ThingInput",
              "description": "Input for a thing",
              "inputFields": [
                { "name": "name", "description": "New name", "defaultValue": "\"none\"",
                  "type": { "kind": "SCALAR", "name": "String", "ofType": null } }
              ]
            },
            {
              "kind": "UNION",
              "name": "SearchResult",
              "possibleTypes": [ { "name": "Thing" } ]
            },
            { "kind": "SCALAR", "name": "String" }
          ]
        }
      }
    }
    """;

    private static List<FileEntry> Generate(bool descriptionAttributes = true)
    {
        var generator = new ClientGenerator("TestNs", "TestClient", false, EnumGeneratorStrategy.FailIfMissing,
            false, false, descriptionAttributes: descriptionAttributes);
        return generator.Generate(SchemaJson);
    }

    private static string File(List<FileEntry> entries, string directory, string fileName) =>
        entries.Single(e => e.DirectoryName == directory && e.FileName == fileName).Content;

    [Fact]
    public void Type_Field_EnumAndInput_GetDescriptionAttributes()
    {
        var entries = Generate();

        var thing = File(entries, "Types", "Thing.cs");
        thing.ShouldContain(Attribute + "\"The name\")]");

        var status = File(entries, "Enums", "Status.cs");
        status.ShouldContain(Attribute + "\"Thing status\")]");
        status.ShouldContain(Attribute + "\"Currently in use\")]");

        var input = File(entries, "Inputs", "ThingInput.cs");
        input.ShouldContain(Attribute + "\"Input for a thing\")]");
        input.ShouldContain(Attribute + "\"New name\")]");
    }

    [Fact]
    public void OperationAndArgument_GetDescriptionAttributes()
    {
        var methods = File(Generate(), "Client", "QueryMethods.cs");

        methods.ShouldContain(Attribute + "\"Get one thing\")]");
        methods.ShouldContain(Attribute + "\"The \\\"id\\\" of the thing\")] string id = null");
    }

    [Fact]
    public void MultiLineDescriptionWithBackslash_IsEscapedAsOneLiteral()
    {
        var thing = File(Generate(), "Types", "Thing.cs");

        thing.ShouldContain(Attribute + "\"A thing.\\nSecond line with a back\\\\slash.\")]");
    }

    [Fact]
    public void NoDescription_EmitsNoAttribute()
    {
        var status = File(Generate(), "Enums", "Status.cs");

        status.Split('\n').Count(l => l.Contains(Attribute)).ShouldBe(2);
    }

    [Fact]
    public void OptOut_EmitsNoAttributesButKeepsSummaries()
    {
        var entries = Generate(descriptionAttributes: false);

        entries.ShouldAllBe(e => !e.Content.Contains(Attribute));
        File(entries, "Types", "Thing.cs").ShouldContain("/// The name");
    }

    [Theory]
    [InlineData("plain", "\"plain\"")]
    [InlineData("say \"hi\"", "\"say \\\"hi\\\"\"")]
    [InlineData("a\\b", "\"a\\\\b\"")]
    [InlineData("one\r\ntwo\nthree", "\"one\\ntwo\\nthree\"")]
    [InlineData("tab\there", "\"tab\\there\"")]
    public void ToStringLiteral_EscapesSpecialCharacters(string text, string expected)
    {
        Helpers.ToStringLiteral(text).ShouldBe(expected);
    }
}
