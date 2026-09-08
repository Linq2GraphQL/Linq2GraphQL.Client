using Linq2GraphQL.Client;
using StarWars.Client;

namespace Linq2GraphQL.Docs.Components.Samples.Queries;

public partial class ProjectCustomObject
{
    private GraphCursorQueryExecute<FilmsConnection, IEnumerable<FilmProjection>> GetQuery()
    {
        return starWarsClient
            .Query
            .AllFilms(first: 3)
            .Select(e => e.Films.Select(e =>
                new FilmProjection { Id = e.Id, Title = e.Title, Created = e.Created, Producers = e.Producers }));
    }
}

public class FilmProjection
{
    public ID Id { get; set; }
    public string Title { get; set; }
    public string Created { get; set; }
    public List<string> Producers { get; set; }
}