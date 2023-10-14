namespace Linq2GraphQL.TestServer.Models;

public class Customer
{
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = "";
    public CustomerStatus Status { get; set; }
    public List<Order> Orders { get; set; } = new();

}

public enum AddressType
{
    Delivery = 0,
    Invoice = 1
}