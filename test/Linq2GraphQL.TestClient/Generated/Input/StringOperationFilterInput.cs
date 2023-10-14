using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;

namespace Linq2GraphQL.TestClient;

[JsonConverter(typeof(GraphInputConverter<StringOperationFilterInput>))]
public partial class StringOperationFilterInput : GraphInputBase
{
	[JsonPropertyName("and")]
	public List<StringOperationFilterInput> And 
	{
		get => GetValue<List<StringOperationFilterInput>>("and");
    	set => SetValue("and", value);
	}

	[JsonPropertyName("or")]
	public List<StringOperationFilterInput> Or 
	{
		get => GetValue<List<StringOperationFilterInput>>("or");
    	set => SetValue("or", value);
	}

	[JsonPropertyName("eq")]
	public string Eq 
	{
		get => GetValue<string>("eq");
    	set => SetValue("eq", value);
	}

	[JsonPropertyName("neq")]
	public string Neq 
	{
		get => GetValue<string>("neq");
    	set => SetValue("neq", value);
	}

	[JsonPropertyName("contains")]
	public string Contains 
	{
		get => GetValue<string>("contains");
    	set => SetValue("contains", value);
	}

	[JsonPropertyName("ncontains")]
	public string Ncontains 
	{
		get => GetValue<string>("ncontains");
    	set => SetValue("ncontains", value);
	}

	[JsonPropertyName("in")]
	public List<string> In 
	{
		get => GetValue<List<string>>("in");
    	set => SetValue("in", value);
	}

	[JsonPropertyName("nin")]
	public List<string> Nin 
	{
		get => GetValue<List<string>>("nin");
    	set => SetValue("nin", value);
	}

	[JsonPropertyName("startsWith")]
	public string StartsWith 
	{
		get => GetValue<string>("startsWith");
    	set => SetValue("startsWith", value);
	}

	[JsonPropertyName("nstartsWith")]
	public string NstartsWith 
	{
		get => GetValue<string>("nstartsWith");
    	set => SetValue("nstartsWith", value);
	}

	[JsonPropertyName("endsWith")]
	public string EndsWith 
	{
		get => GetValue<string>("endsWith");
    	set => SetValue("endsWith", value);
	}

	[JsonPropertyName("nendsWith")]
	public string NendsWith 
	{
		get => GetValue<string>("nendsWith");
    	set => SetValue("nendsWith", value);
	}

}