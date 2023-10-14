using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;

namespace Linq2GraphQL.TestClientNullable;

public partial class Item 
{
	[JsonPropertyName("itemId")]
	public string ItemId { get; set; }  
	[JsonPropertyName("itemName")]
	public string ItemName { get; set; }  

}
