using Linq2GraphQL.Client;
namespace Linq2GraphQL.Docs.Components.Samples.BasicQuery
{
    public partial class ProjectionQuery
    {
        private GraphCursorQueryExecute<StarWars.Client.FilmsConnection, IEnumerable<FilmProjection>> GetQuery()
        {
            return starWarsClient
                .Query
                .AllFilms(first: 3)
                .Include(e => e.Films.Select(f => f.Producers))
                .Select(e => e.Films.Select(e=> new FilmProjection { Title = e.Title, Created = e.Created, Id = e.Id}));
        }
    }

    public class FilmProjection
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Created { get; set; }
    }
}