//---------------------------------------------------------------------
// This code was automatically generated by Linq2GraphQL
// Please don't edit this file
// Github:https://github.com/linq2graphql/linq2graphql.client
// Url: https://linq2graphql.com
//---------------------------------------------------------------------

using Linq2GraphQL.Client;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Linq2GraphQL.TestClientNullable;

public class SampleNullableClient
{
    public SampleNullableClient(HttpClient httpClient, [FromKeyedServices("SampleNullableClient")]IOptions<GraphClientOptions> options, IServiceProvider provider)
    {
        var client = new GraphClient(httpClient, options, provider);
        Query = new QueryMethods(client);
        Mutation = new MutationMethods(client);
    }

    public QueryMethods Query { get; private set; }
    public MutationMethods Mutation { get; private set; }
    
}