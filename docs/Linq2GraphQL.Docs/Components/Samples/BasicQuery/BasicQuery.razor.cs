using Linq2GraphQL.Client;
namespace Linq2GraphQL.Docs.Components.Samples.BasicQuery
{
    public partial class BasicQuery
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