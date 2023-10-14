namespace Linq2GraphQL.TestServer.Models;

public class OrderLine
{
    public OrderLine(Order order)
    {
        Order = order;
    }

    public int LineNumber { get; set; }
    public Order Order { get; set; }

    public Item? Item { get; set; }

    public decimal Price { get; set; }

    public double Quantity { get; set; }
}