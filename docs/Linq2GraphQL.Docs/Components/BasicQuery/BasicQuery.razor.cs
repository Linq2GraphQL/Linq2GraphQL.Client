using Linq2GraphQL.Client;
using Linq2GraphQL.Docs.Components.Samples;

namespace Linq2GraphQL.Docs.Components.BasicQuery
{
    public partial class BasicQuery: SampleBase
    {
        private GraphQueryExecute<StarWars.Client.FilmsConnection, List<StarWars.Client.Film>> GetQuery()
        {
            return starWarsClient
                .Query
                .AllFilms(first: 3)
                .Include(e => e.Films.Select(f => f.Producers))
                .Select(e => e.Films);

        }

    }
}