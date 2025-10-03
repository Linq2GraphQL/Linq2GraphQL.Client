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
    public TimeOnly? EntryTime { get; set; }

    [Obsolete("This propery is obsolete and should not be used!")]
    public string? Grade { get; set; }


    [Obsolete("This method should not be used anymore")]
    public string? GetMyGradeMethod(string name)
    {
        return Grade;
    }

    public string GetOrderHello(string name, int first = 0)
    {
        return $"Hello, {name} [{first}]";
    }

    public Address GetOrderAddress(AddressType addressType)
    {
        return new() { Name = addressType.ToString(), PostalCode = "1234", Street = "Servcie Road 12345" };
    }
}