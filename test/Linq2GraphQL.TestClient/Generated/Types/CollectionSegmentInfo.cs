using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;
using Linq2GraphQL.Client.Common;

namespace Linq2GraphQL.TestClient;

public partial class CollectionSegmentInfo : GraphQLTypeBase
{
    [JsonPropertyName("hasNextPage")]
	public bool HasNextPage { get; set; }  


    [JsonPropertyName("hasPreviousPage")]
	public bool HasPreviousPage { get; set; }  






}
