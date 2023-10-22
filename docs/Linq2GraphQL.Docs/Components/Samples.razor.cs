using Microsoft.AspNetCore.Components;
using StarWars.Client;

namespace Linq2GraphQL.Docs.Components
{
    public partial class Samples
    {
        private List<Film> films;

        [Inject] private StarWarsClient starWarsClient { get; set; }
        protected override async Task OnInitializedAsync()
        {

            films = await starWarsClient
               .Query
               .AllFilms(first: 100)
               .Select(e => e.Films)
               .ExecuteAsync();

            var tt = films;
            await base.OnInitializedAsync();
        }

    }
}