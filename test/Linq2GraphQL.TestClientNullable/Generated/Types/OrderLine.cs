using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;
using Linq2GraphQL.Client.Common;

namespace Linq2GraphQL.TestClientNullable;

public partial class OrderLine : GraphQLTypeBase
{
    [JsonPropertyName("lineNumber")]
	public required int LineNumber { get; set; }  

    [JsonPropertyName("order")]
	public required Order Order { get; set; }  

    [JsonPropertyName("item")]
	public Item? Item { get; set; }  

    [JsonPropertyName("price")]
	public required decimal Price { get; set; }  

    [JsonPropertyName("quantity")]
	public required double Quantity { get; set; }  

}
