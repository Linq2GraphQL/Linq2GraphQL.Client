//---------------------------------------------------------------------
// This code was automatically generated by Linq2GraphQL
// Please don't edit this file
// Github:https://github.com/linq2graphql/linq2graphql.client
// Url: https://linq2graphql.com
//---------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;
using Linq2GraphQL.Client.Common;

namespace Linq2GraphQL.TestClient;


public partial class Pig : GraphQLTypeBase, IAnimal
{
    [JsonPropertyName("name")]
    [GraphQLMember("name")]
    public string Name { get; set; }

    [JsonPropertyName("numberOfLegs")]
    [GraphQLMember("numberOfLegs")]
    public int NumberOfLegs { get; set; }

    [JsonPropertyName("speed")]
    [GraphQLMember("speed")]
    public int Speed { get; set; }

    [JsonPropertyName("spices")]
    [GraphQLMember("spices")]
    public string Spices { get; set; }

    [JsonPropertyName("__typename")]
    public string __TypeName { get; set; }
}
