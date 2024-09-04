using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;
using Linq2GraphQL.Client.Common;

namespace Linq2GraphQL.TestClientNullable;

public partial class Item : GraphQLTypeBase
{
    [JsonPropertyName("itemId")]
	public required string ItemId { get; set; }  

    [JsonPropertyName("itemName")]
	public required string ItemName { get; set; }  

}
