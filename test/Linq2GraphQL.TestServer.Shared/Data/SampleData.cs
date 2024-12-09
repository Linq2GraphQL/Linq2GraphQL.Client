using Linq2GraphQL.TestServer.Models;
using System.Text;

namespace Linq2GraphQL.TestServer.Data;

public static class SampleData
{
    public static List<IAnimal> GetAnimals()
    {
        var result = new List<IAnimal>();
        result.Add(new Pig { Name = "Kalle", NumberOfLegs = 4, Speed = 5, Spices ="Normal"  });
        result.Add(new Spider { Name = "Imse", NumberOfLegs = 8, Speed= 11, Poisonous = true});
        return result;
    }


    public static Item GetItem(string ItemId)
    {
        var itemName = $"Item: {ItemId}";
        return new Item { ItemId = ItemId, ItemName = itemName, Data = Encoding.ASCII.GetBytes(itemName) };
    }

    public static List<Customer> GetCustomers()
    {
        var customers = new List<Customer>();

        var cust1 = new Customer
        {
            CustomerId = Guid.Parse("258f2814-c938-484a-884f-8748b10ca9f9"),
            CustomerName = "Universal Ltd",
            Status = CustomerStatus.Active,
           
        };

        cust1.Orders.Add(GenerateOrder1(cust1));
        cust1.Orders.Add(GenerateOrder2(cust1));
        customers.Add(cust1);

        var cust2 = new Customer
        {
            CustomerId = Guid.Parse("3b1761fb-6551-404e-80b0-a6c12f298a06"),
            CustomerName = "Jocke Pocke",
            Status = CustomerStatus.Disabled,
            Address = new Address(){ Name = "JockeAddr", PostalCode = "11111", Street = "Street 123"}
        };

        cust2.Orders.Add(GenerateOrder3(cust2));
        cust2.Orders.Add(GenerateOrder4(cust2));
        customers.Add(cust2);


        return customers;
    }

    private static Order GenerateOrder1(Customer customer)
    {
        var order = new Order(customer)
        {
            OrderId =  Guid.Parse("fcdf94de-403e-48ea-b5cb-383bb67694d0"),
            OrderDate = new DateTimeOffset(2021, 4, 11, 2, 12, 10, 122, new TimeSpan(1, 0, 0)),
            Address = new Address { Name = "Garden Inc", PostalCode = "1234", Street = "New Street 11" }
        };


        var line1 = new OrderLine(order)
        {
            LineNumber = 1,
            Price = 56.4M,
            Quantity = 10,
            Item = GetItem("12345")
        };
        order.Lines.Add(line1);

        var line2 = new OrderLine(order)
        {
            LineNumber = 2,
            Price = 11.4M,
            Quantity = 123.4,
            Item = GetItem("ABCDE")
        };
        order.Lines.Add(line2);

        return order;
    }

    private static Order GenerateOrder2(Customer customer)
    {
        var order = new Order(customer)
        {
            OrderId = Guid.Parse("80bb321a-3d00-4bd1-be55-47faf07977ac"),
            OrderDate = new DateTimeOffset(2022, 06, 21, 6, 33, 1, 221, new TimeSpan(3, 0, 0)),
            Address = new Address { Name = "Justify AB", PostalCode = "54354", Street = "Holy Street" }
        };


        var line1 = new OrderLine(order)
        {
            LineNumber = 1,
            Price = 56.4M,
            Quantity = 10,
            Item = GetItem("12345")
        };
        order.Lines.Add(line1);

        var line2 = new OrderLine(order)
        {
            LineNumber = 2,
            Price = 11.4M,
            Quantity = 123.4,
            Item = GetItem("ABCDE")
        };
        order.Lines.Add(line2);


        var line3 = new OrderLine(order)
        {
            LineNumber = 3,
            Price = 2345.50M,
            Quantity = 400,
            Item = GetItem("AA1234")
        };
        order.Lines.Add(line3);

        return order;
    }

    private static Order GenerateOrder3(Customer customer)
    {
        var order = new Order(customer)
        {
            OrderId = Guid.Parse("ba8262aa-0b50-40f6-80f0-b4ae474bbf9a"),
            OrderDate = new DateTimeOffset(2023, 06, 21, 6, 33, 1, 221, new TimeSpan(3, 0, 0)),
            Address = new Address { Name = "Net Box 123", PostalCode = "123", Street = "Bang Street 99" }
        };


        var line1 = new OrderLine(order)
        {
            LineNumber = 1,
            Price = 5,
            Quantity = 10,
            Item = GetItem("ABC22")
        };
        order.Lines.Add(line1);

        var line2 = new OrderLine(order)
        {
            LineNumber = 2,
            Price = 111.4M,
            Quantity = 11,
            Item = GetItem("Vent")
        };
        order.Lines.Add(line2);



        return order;
    }

    private static Order GenerateOrder4(Customer customer)
    {
        var order = new Order(customer)
        {
            OrderId = Guid.Parse("4a0ecfc8-eec8-49bb-812e-6866157954c0"),
            OrderDate = new DateTimeOffset(2021, 06, 21, 6, 33, 1, 221, new TimeSpan(3, 0, 0)),
            Address = new Address { Name = "Delivery Street", PostalCode = "14332", Street = "Checker Street" }
        };


        var line1 = new OrderLine(order)
        {
            LineNumber = 1,
            Price = 99.3M,
            Quantity = 100,
            Item = GetItem("Main Unit")
        };
        order.Lines.Add(line1);

        var line2 = new OrderLine(order)
        {
            LineNumber = 2,
            Price = 111.4M,
            Quantity = 11,
            Item = GetItem("Vent")
        };
        order.Lines.Add(line2);



        return order;
    }

}