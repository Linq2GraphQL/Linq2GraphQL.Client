using Linq2GraphQL.Client;
using StarWars.Client;

namespace Linq2GraphQL.Docs.Components.Samples.Queries;

public partial class SelectMany
{
    private GraphCursorQueryExecute<FilmsConnection, IEnumerable<string>> GetQuery()
    {
        return starWarsClient
            .Query
            .AllFilms()
            .Select(e => e.Films.SelectMany(e => e.Producers)
            );
    }
}