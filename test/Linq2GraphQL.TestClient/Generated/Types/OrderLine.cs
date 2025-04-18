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


public partial class OrderLine : GraphQLTypeBase
{
    [GraphQLMember("lineNumber")]
    [JsonPropertyName("lineNumber")]
    public int LineNumber { get; set; }

    [GraphQLMember("order")]
    [JsonPropertyName("order")]
    public Order Order { get; set; }

    [GraphQLMember("item")]
    [JsonPropertyName("item")]
    public Item Item { get; set; }

    [GraphQLMember("price")]
    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [GraphQLMember("quantity")]
    [JsonPropertyName("quantity")]
    public double Quantity { get; set; }

}
