using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;

namespace Linq2GraphQL.TestClient;

public partial class AnimalsEdge 
{
	[JsonPropertyName("cursor")]
	public string Cursor { get; set; }  

	[JsonPropertyName("node")]
	public IAnimal Node { get; set; }  


}
