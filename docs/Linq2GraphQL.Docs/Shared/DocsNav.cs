namespace Linq2GraphQL.Docs.Shared;

public record DocPage(string Href, string Title, string SourceFile);

public record DocSection(string Title, IReadOnlyList<DocPage> Pages);

public static class DocsNav
{
    public const string RepositoryUrl = "https://github.com/Linq2GraphQL/Linq2GraphQL.Client";
    public const string NuGetUrl = "https://www.nuget.org/packages/Linq2GraphQL.Client";

    public static readonly IReadOnlyList<DocSection> Sections =
    [
        new("Overview",
        [
            new("", "Introduction", "Pages/Index.razor"),
            new("getting-started", "Getting started", "Pages/GettingStarted.razor")
        ]),
        new("Guides",
        [
            new("selecting-fields", "Selecting fields", "Pages/SelectingFields.razor"),
            new("error-handling", "Error handling", "Pages/ErrorHandling.razor"),
            new("paging", "Paging", "Pages/Paging.razor"),
            new("batching", "Batching", "Pages/Batching.razor"),
            new("subscriptions", "Subscriptions", "Pages/Subscriptions.razor"),
            new("agent-mode", "Agent mode", "Pages/AgentMode.razor")
        ]),
        new("Tools",
        [
            new("generator", "Generator reference", "Pages/Generator.razor"),
            new("generate-client", "Online generator", "Components/GenerateClient.razor")
        ])
    ];

    public static readonly DocPage[] Pages = Sections.SelectMany(section => section.Pages).ToArray();

    public static int IndexOf(string relativePath) =>
        Array.FindIndex(Pages, page => page.Href == NormalizePath(relativePath));

    public static string NormalizePath(string relativePath) =>
        relativePath.Split('?', '#')[0].Trim('/');

    public static string SourceUrl(DocPage page) =>
        $"{RepositoryUrl}/blob/main/docs/Linq2GraphQL.Docs/{page.SourceFile}";
}
