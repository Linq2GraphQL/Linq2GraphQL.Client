using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;
using Linq2GraphQL.Client.Common;

namespace Linq2GraphQL.TestClientNullable;

public static class OrderExtensions
{
    [GraphMethod("orderHello")]
    public static required string OrderHello(this Order  order, [GraphArgument("String!")] string name, [GraphArgument("Int!")] int first)
    {
	    return order.GetMethodValue<required string>("orderHello", name, first);
    }

    [GraphMethod("orderAddress")]
    public static required Address OrderAddress(this Order  order, [GraphArgument("AddressType!")] AddressType addressType)
    {
	    return order.GetMethodValue<required Address>("orderAddress", addressType);
    }

}

public partial class Order : GraphQLTypeBase
{
    private LazyProperty<required string> _orderHello = new();
    /// <summary>
    /// Do not use in Query, only to retrive result
    /// </summary>
    [GraphShadowProperty]
    public required string OrderHello => _orderHello.Value(() => GetFirstMethodValue<required string>("orderHello"));

    private LazyProperty<required Address> _orderAddress = new();
    /// <summary>
    /// Do not use in Query, only to retrive result
    /// </summary>
    [GraphShadowProperty]
    public required Address OrderAddress => _orderAddress.Value(() => GetFirstMethodValue<required Address>("orderAddress"));

    [JsonPropertyName("orderId")]
	public required Guid OrderId { get; set; }  

    [JsonPropertyName("customer")]
	public required Customer Customer { get; set; }  

    [JsonPropertyName("address")]
	public Address? Address { get; set; }  

    [JsonPropertyName("orderDate")]
	public required DateTimeOffset OrderDate { get; set; }  

    [JsonPropertyName("lines")]
	public required List<OrderLine> Lines { get; set; }  

    [JsonPropertyName("entryTime")]
	public TimeSpan? EntryTime { get; set; }  

}
