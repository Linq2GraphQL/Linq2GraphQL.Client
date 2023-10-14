using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;

namespace Linq2GraphQL.TestClient;

public partial class Address 
{
	[JsonPropertyName("name")]
	public string Name { get; set; }  

	[JsonPropertyName("street")]
	public string Street { get; set; }  

	[JsonPropertyName("postalCode")]
	public string PostalCode { get; set; }  


}
