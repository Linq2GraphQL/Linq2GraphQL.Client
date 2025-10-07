using Linq2GraphQL.Client;
using StarWars.Client;

namespace Linq2GraphQL.Docs.Components.Samples.Queries;

public partial class BasicQuery
{
    private GraphQueryExecute<FilmsConnection, List<Film>> GetQuery()
    {
        return starWarsClient
            .Query
            .AllFilms(first: 3)
            .Include(e => e.Films.Select(f => f.Producers))
            .Select(e => e.Films);
    }
}