namespace Linq2GraphQL.TestServer.Models;

public class Order
{
    public Order(Customer customer)
    {
        Customer = customer;
    }

    public Guid OrderId { get; set; } = Guid.NewGuid();
    public Customer Customer { get; set; }

    public Address? Address { get; set; }
    public DateTimeOffset OrderDate { get; set; }
    public List<OrderLine> Lines { get; set; } = new();

#pragma warning disable CA1822 // Mark members as static
    public string GetOrderHello(string name)
#pragma warning restore CA1822 // Mark members as static
    {
        return $"Hello, {name}";
    }

    public Address GetOrderAddress(AddressType addressType)
    {
        return new Address { Name = addressType.ToString(), PostalCode = "1234", Street = "Servcie Road 12345" };
    }

}