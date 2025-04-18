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

namespace Linq2GraphQL.TestClientNullable;

#pragma warning disable CS8618

public partial class Customer : GraphQLTypeBase
{
    [GraphQLMember("customerId")]
    [JsonPropertyName("customerId")]
    public Guid CustomerId { get; set; }

    [GraphQLMember("customerName")]
    [JsonPropertyName("customerName")]
    public string CustomerName { get; set; }

    [GraphQLMember("status")]
    [JsonPropertyName("status")]
    public CustomerStatus Status { get; set; }

    [GraphQLMember("orders")]
    [JsonPropertyName("orders")]
    public List<Order> Orders { get; set; }

    [GraphQLMember("address")]
    [JsonPropertyName("address")]
    public Address? Address { get; set; }

}
