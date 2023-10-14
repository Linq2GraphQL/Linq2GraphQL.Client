using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;

namespace Linq2GraphQL.TestClient;

[JsonConverter(typeof(GraphInputConverter<IAnimalFilterInput>))]
public partial class IAnimalFilterInput : GraphInputBase
{
	[JsonPropertyName("and")]
	public List<IAnimalFilterInput> And 
	{
		get => GetValue<List<IAnimalFilterInput>>("and");
    	set => SetValue("and", value);
	}

	[JsonPropertyName("or")]
	public List<IAnimalFilterInput> Or 
	{
		get => GetValue<List<IAnimalFilterInput>>("or");
    	set => SetValue("or", value);
	}

	[JsonPropertyName("name")]
	public StringOperationFilterInput Name 
	{
		get => GetValue<StringOperationFilterInput>("name");
    	set => SetValue("name", value);
	}

	[JsonPropertyName("numberOfLegs")]
	public IntOperationFilterInput NumberOfLegs 
	{
		get => GetValue<IntOperationFilterInput>("numberOfLegs");
    	set => SetValue("numberOfLegs", value);
	}

}