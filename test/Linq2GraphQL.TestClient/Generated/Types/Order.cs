using System.Text.Json.Serialization;
using Linq2GraphQL.Client;

namespace Linq2GraphQL.TestClient;

public static class OrderExtensions
{
    [GraphMethod("orderHello")]
    public static string OrderHello(this Order  order, [GraphArgument("String!")] string name)
    {
	    return order?.OrderHello;
    }

    [GraphMethod("orderAddress")]
    public static Address OrderAddress(this Order  order, [GraphArgument("AddressType!")] AddressType addressType)
    {
	    return order?.OrderAddress;
    }

}

public partial class Order 
{
    /// <summary>
    /// Do not use in Query, only to retrive result
    /// </summary>
    [GraphQLShadowProperty]
	[JsonPropertyName("orderHello")]
	public string OrderHello { get; set; }  

    /// <summary>
    /// Do not use in Query, only to retrive result
    /// </summary>
    [GraphQLShadowProperty]
	[JsonPropertyName("orderAddress")]
	public Address OrderAddress { get; set; }  

	[JsonPropertyName("orderId")]
	public Guid OrderId { get; set; }  

	[JsonPropertyName("customer")]
	public Customer Customer { get; set; }  

	[JsonPropertyName("address")]
	public Address Address { get; set; }  

	[JsonPropertyName("orderDate")]
	public DateTimeOffset OrderDate { get; set; }  

	[JsonPropertyName("lines")]
	public List<OrderLine> Lines { get; set; }  


}
