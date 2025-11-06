using Linq2GraphQL.Client;
using StarWars.Client;

namespace Linq2GraphQL.Docs.Components.Samples.Interfaces;

public partial class InterfaceQuery
{
    private GraphQueryExecute<Node, Node> GetQuery()
    {
        return starWarsClient
            .Query
            .Node(new ID { Value = "ZmlsbXM6MQ==" })
            .Include(e => e.Film())
            .Select();
    }
}