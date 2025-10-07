using Microsoft.AspNetCore.Components;
using StarWars.Client;

namespace Linq2GraphQL.Docs.Components.Samples;

public class SampleBase : ComponentBase
{
    [Inject] public StarWarsClient starWarsClient { get; set; }

    public string GetTypeFullName()
    {
        return GetType().FullName;
    }
}