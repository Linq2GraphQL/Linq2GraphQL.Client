using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;
using Linq2GraphQL.Client.Common;

namespace Linq2GraphQL.TestClient;

public partial class OrdersEdge : GraphQLTypeBase
{
    [JsonPropertyName("cursor")]
	public string Cursor { get; set; }  


    [JsonPropertyName("node")]
	public Order Node { get; set; }  






}
