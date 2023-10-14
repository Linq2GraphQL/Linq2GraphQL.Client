using Linq2GraphQL.TestServer.Data;
using Linq2GraphQL.TestServer.Models;

namespace Linq2GraphQL.TestServer;

public class Query
{
    public string Hello(string name = "World")
    {
        return $"Hello, {name}!";
    }

    public Customer GetCustomerReturnNull()
    {
        return null;
    }

    public List<Customer> GetCustomers()
    {
        return SampleData.GetCustomers();
    }

    [UsePaging(IncludeTotalCount = true, AllowBackwardPagination = false)]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Order> GetOrdersNoBackwardPagination()
    {
        return SampleData.GetCustomers().SelectMany(e => e.Orders).Distinct().AsQueryable();
    }

    [UsePaging(IncludeTotalCount = false)]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Order> GetOrdersNoTotalCount()
    {
        return SampleData.GetCustomers().SelectMany(e => e.Orders).Distinct().AsQueryable();
    }

    [UsePaging(MaxPageSize = 2, IncludeTotalCount = true)]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Order> GetOrders()
    {
        return SampleData.GetCustomers().SelectMany(e => e.Orders).Distinct().AsQueryable();
    }

    [UsePaging(MaxPageSize = 200, IncludeTotalCount = true)]
    [UseFiltering]
    [UseSorting]
    public IQueryable<IAnimal> GetAnimals()
    {
        return SampleData.GetAnimals().AsQueryable();
    }

    


    [UseOffsetPaging(DefaultPageSize = 200, IncludeTotalCount = true)]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Order> GetOrdersOffsetPaging()
    {
        return SampleData.GetCustomers().SelectMany(e => e.Orders).Distinct().AsQueryable();
    }

}