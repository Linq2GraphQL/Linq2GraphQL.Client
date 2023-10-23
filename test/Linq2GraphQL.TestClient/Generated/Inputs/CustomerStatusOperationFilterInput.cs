using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;

namespace Linq2GraphQL.TestClient;

[JsonConverter(typeof(GraphInputConverter<CustomerStatusOperationFilterInput>))]
public partial class CustomerStatusOperationFilterInput : GraphInputBase
{
	[JsonPropertyName("eq")]
	public CustomerStatus? Eq 
	{
		get => GetValue<CustomerStatus?>("eq");
    	set => SetValue("eq", value);
	}

	[JsonPropertyName("neq")]
	public CustomerStatus? Neq 
	{
		get => GetValue<CustomerStatus?>("neq");
    	set => SetValue("neq", value);
	}

	[JsonPropertyName("in")]
	public List<CustomerStatus> In 
	{
		get => GetValue<List<CustomerStatus>>("in");
    	set => SetValue("in", value);
	}

	[JsonPropertyName("nin")]
	public List<CustomerStatus> Nin 
	{
		get => GetValue<List<CustomerStatus>>("nin");
    	set => SetValue("nin", value);
	}

}