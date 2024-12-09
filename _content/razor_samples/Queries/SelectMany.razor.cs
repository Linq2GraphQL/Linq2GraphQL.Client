using Linq2GraphQL.Client;
namespace Linq2GraphQL.Docs.Components.Samples.Queries
{
    public partial class SelectMany
    {
        private GraphCursorQueryExecute<StarWars.Client.FilmsConnection, IEnumerable<string>> GetQuery()
        {
            return starWarsClient
                .Query
                .AllFilms()
                .Select(e => e.Films.SelectMany(e => e.Producers)
                );
        }
    }
}