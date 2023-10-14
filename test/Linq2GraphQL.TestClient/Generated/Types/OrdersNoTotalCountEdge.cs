using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;

namespace Linq2GraphQL.TestClient;

public partial class OrdersNoTotalCountEdge 
{
	[JsonPropertyName("cursor")]
	public string Cursor { get; set; }  

	[JsonPropertyName("node")]
	public Order Node { get; set; }  


}
