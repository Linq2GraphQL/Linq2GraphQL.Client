using Linq2GraphQL.Client;
using Microsoft.Extensions.Options;
using StartGG.Client;

namespace StartGG
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer ***************");
            httpClient.BaseAddress = new Uri("https://api.start.gg/gql/alpha");

            IOptions<GraphClientOptions> myOptions = Options.Create<GraphClientOptions>(new GraphClientOptions());

            var ggClient = new StartGGClient(httpClient, myOptions, null);

            //var eventWithTournament = await ggClient
            //    .Query
            //    .Event(null, "tournament/quickdraw-brawl-26/event/bbcf-double-elimination")
            //    .Include(e => e.Tournament)
            //    .Select()
            //    .ExecuteAsync();

            //var query = ggClient
            //    .Query
            //    .Tournaments(new TournamentQuery { PerPage = 5, Page = 1, Filter = new TournamentPageFilter { Name = "Genesis" } })
            //    //.Include(e => e.Nodes)
            //    //.Include(e => e.Nodes.Select(e => e.Events(null, null)))
            //    //.Include(e => e.Nodes.Select(e => e.Events(null, null).Select(e => e.Standings(new StandingPaginationQuery { Page = 1, PerPage = 4 }).Nodes)))

            //    .Select(e => e.Nodes.Select(e=> e.Events(null, null)));
            //var request = await query.GetRequestAsJsonAsync();

            //var result = await query.ExecuteAsync();

            var infoStoragesQuery = ggClient
                .Query
                 .Tournaments(new TournamentQuery { PerPage = 5, Page = 1, Filter = new TournamentPageFilter { Name = "Genesis" } })
                 .Select(e => e.Nodes.Select(t => t.Events(null, null)).SelectMany(evl => evl.Select(ev => new InfoStorage { numAttendees = ev.NumEntrants, slug = ev.Slug })));



var jj = await infoStoragesQuery.GetRequestAsJsonAsync();



          

        }
    }

    public class InfoStorage
    {
        public int? numAttendees { get; set; }
        public string slug { get; set; }
        public IEnumerable<string> topPlacers { get; set; }
    }

}
