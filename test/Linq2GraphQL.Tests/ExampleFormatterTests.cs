using Linq2GraphQL.Client.AI.Schema;
using Shouldly;

namespace Linq2GraphQL.Tests;

/// <summary>
/// Covers how examples built by the typed client are tidied before the agent sees them.
/// </summary>
public class ExampleFormatterTests
{
    [Fact]
    public void Tidy_RemovesUniquenessSuffixesAndNormalisesSpacing()
    {
        var (query, variables) = ExampleFormatter.Tidy("query($name_1_1: String ){  hello(name:$name_1_1 )  }",
            new Dictionary<string, object> { ["name_1_1"] = "Jocke" });

        query.ShouldBe("query($name: String) { hello(name: $name) }");
        variables.ShouldBe(new Dictionary<string, object> { ["name"] = "Jocke" });
    }

    [Fact]
    public void Tidy_KeepsSuffixesWhenPlainNamesWouldClash()
    {
        var (query, variables) = ExampleFormatter.Tidy(
            "query($first_1_1: Int, $first_2_1: Int) { a: orders(first: $first_1_1) { totalCount } b: orders(first: $first_2_1) { totalCount } }",
            new Dictionary<string, object> { ["first_1_1"] = 1, ["first_2_1"] = 2 });

        query.ShouldBe("query($first_1_1: Int, $first_2_1: Int) { a: orders(first: $first_1_1) { totalCount } b: orders(first: $first_2_1) { totalCount } }");
        variables.Keys.ShouldBe(["first_1_1", "first_2_1"]);
    }

    [Fact]
    public void Tidy_LeavesStringLiteralsAlone()
    {
        var (query, _) = ExampleFormatter.Tidy("{ hello(name:   \"two  spaces\") }", null);

        query.ShouldBe("{ hello(name: \"two  spaces\") }");
    }

    [Fact]
    public void Tidy_PrintsNestedSelectionsOnOneLine()
    {
        var (query, _) = ExampleFormatter.Tidy("query{\n  customers {\n   customerName\n   status }\n  }", null);

        query.ShouldBe("{ customers { customerName status } }");
    }
}
