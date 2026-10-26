using Linq2GraphQL.Client;
using Linq2GraphQL.TestClient;

namespace Linq2GraphQL.Tests.Batch;

/// <summary>Golden copies of the merged documents a batch sends.</summary>
public class BatchRequestSnapshotTests : IDisposable
{
    private readonly BatchTestServer server = new();
    private readonly SampleClient client;

    public BatchRequestSnapshotTests()
    {
        client = server.CreateClient();
    }

    public void Dispose()
    {
        server.Dispose();
    }

    [Fact]
    public async Task Queries_WithArgumentsAtSeveralLevels()
    {
        await client.CreateBatch()
            .Query(c => c.Query.Hello("Jocke").Select().OnResult(_ => { }))
            .Query(c => c.Query.Hello().Select().OnResult(_ => { }))
            .Query(c => c.Query.Orders(1)
                .Include(e => e.Nodes.Select(n => n.OrderHello("Jocke", 2)))
                .Include(e => e.Nodes.Select(n => n.OrderAddress(AddressType.Invoice)))
                .Select(e => e.Nodes)
                .OnResult(_ => { }))
            .Query(c => c.Query.Orders()
                .Select(e => e.Nodes.Select(n => new { A = n.OrderHello("A", 1), B = n.OrderHello("B", 2) }))
                .OnResult(_ => { }))
            .ExecuteAsync();

        MatchesSnapshot("Queries_WithArgumentsAtSeveralLevels");
    }

    [Fact]
    public async Task Mutations()
    {
        await client.CreateBatch()
            .Query(c => c.Mutation.SetName("A").Select().OnResult(_ => { }))
            .Query(c => c.Mutation.SetName("B").Select().OnResult(_ => { }))
            .ExecuteAsync();

        MatchesSnapshot("Mutations");
    }

    private void MatchesSnapshot(string name)
    {
        var operation = Assert.Single(server.Operations);
        Snapshot.Match(operation.Query + "\n\n" + operation.Variables, "Batch." + name);
    }
}
