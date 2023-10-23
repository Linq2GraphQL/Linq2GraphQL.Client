using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;

namespace Linq2GraphQL.TestClient;

[JsonConverter(typeof(GraphInputConverter<UuidOperationFilterInput>))]
public partial class UuidOperationFilterInput : GraphInputBase
{
	[JsonPropertyName("eq")]
	public Guid? Eq 
	{
		get => GetValue<Guid?>("eq");
    	set => SetValue("eq", value);
	}

	[JsonPropertyName("neq")]
	public Guid? Neq 
	{
		get => GetValue<Guid?>("neq");
    	set => SetValue("neq", value);
	}

	[JsonPropertyName("in")]
	public List<Guid?> In 
	{
		get => GetValue<List<Guid?>>("in");
    	set => SetValue("in", value);
	}

	[JsonPropertyName("nin")]
	public List<Guid?> Nin 
	{
		get => GetValue<List<Guid?>>("nin");
    	set => SetValue("nin", value);
	}

	[JsonPropertyName("gt")]
	public Guid? Gt 
	{
		get => GetValue<Guid?>("gt");
    	set => SetValue("gt", value);
	}

	[JsonPropertyName("ngt")]
	public Guid? Ngt 
	{
		get => GetValue<Guid?>("ngt");
    	set => SetValue("ngt", value);
	}

	[JsonPropertyName("gte")]
	public Guid? Gte 
	{
		get => GetValue<Guid?>("gte");
    	set => SetValue("gte", value);
	}

	[JsonPropertyName("ngte")]
	public Guid? Ngte 
	{
		get => GetValue<Guid?>("ngte");
    	set => SetValue("ngte", value);
	}

	[JsonPropertyName("lt")]
	public Guid? Lt 
	{
		get => GetValue<Guid?>("lt");
    	set => SetValue("lt", value);
	}

	[JsonPropertyName("nlt")]
	public Guid? Nlt 
	{
		get => GetValue<Guid?>("nlt");
    	set => SetValue("nlt", value);
	}

	[JsonPropertyName("lte")]
	public Guid? Lte 
	{
		get => GetValue<Guid?>("lte");
    	set => SetValue("lte", value);
	}

	[JsonPropertyName("nlte")]
	public Guid? Nlte 
	{
		get => GetValue<Guid?>("nlte");
    	set => SetValue("nlte", value);
	}

}