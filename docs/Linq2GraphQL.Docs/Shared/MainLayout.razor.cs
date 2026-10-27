using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;

namespace Linq2GraphQL.Docs.Shared;

public partial class MainLayout : IDisposable
{
    private List<Heading> headings = [];
    private bool menuOpen;
    private bool observeHeadingsAfterRender;
    private string pendingFragment;
    private bool refreshHeadings = true;
    private string theme = "light";

    [Inject] private NavigationManager Navigation { get; set; }
    [Inject] private IJSRuntime JS { get; set; }

    private static string Version => ThisAssembly.AssemblyInformationalVersion.Split('+')[0];

    private string CurrentPath => DocsNav.NormalizePath(Navigation.ToBaseRelativePath(Navigation.Uri));

    private bool IsLanding => CurrentPath == "";

    private bool IsGuidePage => !IsLanding && CurrentPath != "generate-client";

    private DocPage CurrentPage => PageAt(DocsNav.IndexOf(CurrentPath));

    private DocPage PreviousPage => PageAt(DocsNav.IndexOf(CurrentPath) - 1);

    private DocPage NextPage => PageAt(DocsNav.IndexOf(CurrentPath) is var index and >= 0 ? index + 1 : -1);

    public void Dispose() => Navigation.LocationChanged -= OnLocationChanged;

    protected override void OnInitialized()
    {
        pendingFragment = FragmentOf(Navigation.Uri);
        Navigation.LocationChanged += OnLocationChanged;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            theme = await JS.InvokeAsync<string>("l2gDocs.getTheme");
        }

        if (refreshHeadings)
        {
            refreshHeadings = false;
            headings = await JS.InvokeAsync<List<Heading>>("l2gDocs.readHeadings");
            observeHeadingsAfterRender = true;
            StateHasChanged();
            return;
        }

        if (observeHeadingsAfterRender)
        {
            observeHeadingsAfterRender = false;
            await JS.InvokeVoidAsync("l2gDocs.observeHeadings");
            if (pendingFragment is not null)
            {
                await JS.InvokeVoidAsync("l2gDocs.scrollToHeading", pendingFragment);
                pendingFragment = null;
            }
        }
    }

    private void OnLocationChanged(object sender, LocationChangedEventArgs e) =>
        _ = InvokeAsync(async () =>
        {
            menuOpen = false;
            pendingFragment = FragmentOf(e.Location);
            refreshHeadings = true;
            StateHasChanged();
            if (pendingFragment is null)
            {
                try
                {
                    await JS.InvokeVoidAsync("l2gDocs.scrollToTop");
                }
                catch (JSException)
                {
                }
            }
        });

    private async Task ToggleThemeAsync()
    {
        theme = theme == "dark" ? "light" : "dark";
        await JS.InvokeVoidAsync("l2gDocs.setTheme", theme);
    }

    private void ToggleMenu() => menuOpen = !menuOpen;

    private void CloseMenu() => menuOpen = false;

    private async Task ScrollToHeadingAsync(string id) =>
        await JS.InvokeVoidAsync("l2gDocs.scrollToHeading", id);

    private static DocPage PageAt(int index) =>
        index >= 0 && index < DocsNav.Pages.Length ? DocsNav.Pages[index] : null;

    private static string FragmentOf(string uri)
    {
        var hashIndex = uri.IndexOf('#');
        return hashIndex >= 0 && hashIndex < uri.Length - 1 ? uri[(hashIndex + 1)..] : null;
    }

    private record Heading(string Id, string Text);
}
