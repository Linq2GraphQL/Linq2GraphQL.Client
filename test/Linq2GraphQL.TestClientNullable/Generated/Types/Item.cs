using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;
using Linq2GraphQL.Client.Common;

namespace Linq2GraphQL.TestClientNullable;

#pragma warning disable CS8618

public partial class Item : GraphQLTypeBase
{
    [JsonPropertyName("itemId")]
	public string ItemId { get; set; }  

    [JsonPropertyName("itemName")]
	public string ItemName { get; set; }  

}
