using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;

namespace Linq2GraphQL.TestClient;

public partial class CollectionSegmentInfo 
{
	[JsonPropertyName("hasNextPage")]
	public bool HasNextPage { get; set; }  

	[JsonPropertyName("hasPreviousPage")]
	public bool HasPreviousPage { get; set; }  


}
