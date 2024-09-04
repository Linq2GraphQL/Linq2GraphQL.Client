using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;

namespace Linq2GraphQL.TestClientNullable;

public static class IF 
{ 
	public static AddressInput Address() 
	{
		return new AddressInput();
	}
	public static CustomerInput Customer() 
	{
		return new CustomerInput();
	}
	public static ItemInput Item() 
	{
		return new ItemInput();
	}
	public static OrderInput Order() 
	{
		return new OrderInput();
	}
	public static OrderLineInput OrderLine() 
	{
		return new OrderLineInput();
	}
}



public static class AddressInputExtensions
{ 
	
    public static AddressInput Name(this AddressInput input, string val)
    {
         input.Name = val;
         return input;
    }


    public static AddressInput Street(this AddressInput input, string val)
    {
         input.Street = val;
         return input;
    }


    public static AddressInput PostalCode(this AddressInput input, string val)
    {
         input.PostalCode = val;
         return input;
    }

}

public static class CustomerInputExtensions
{ 
	
    public static CustomerInput CustomerId(this CustomerInput input, Guid val)
    {
         input.CustomerId = val;
         return input;
    }


    public static CustomerInput CustomerName(this CustomerInput input, string val)
    {
         input.CustomerName = val;
         return input;
    }


    public static CustomerInput Status(this CustomerInput input, CustomerStatus val)
    {
         input.Status = val;
         return input;
    }

    public static CustomerInput Orders(this CustomerInput input, Action<List<OrderInput>> mod)
    {
        var filter = new List<OrderInput>();
        mod ??= _ => { };
        mod(filter); 
        input.Orders = filter;
        return input;
    }

    public static CustomerInput Address(this CustomerInput input, Action<AddressInput?> mod)
    {
        var filter = new AddressInput?();
        mod ??= _ => { };
        mod(filter); 
        input.Address = filter;
        return input;
    }

}

public static class ItemInputExtensions
{ 
	
    public static ItemInput ItemId(this ItemInput input, string val)
    {
         input.ItemId = val;
         return input;
    }


    public static ItemInput ItemName(this ItemInput input, string val)
    {
         input.ItemName = val;
         return input;
    }

}

public static class OrderInputExtensions
{ 
	
    public static OrderInput OrderId(this OrderInput input, Guid val)
    {
         input.OrderId = val;
         return input;
    }

    public static OrderInput Customer(this OrderInput input, Action<CustomerInput> mod)
    {
        var filter = new CustomerInput();
        mod ??= _ => { };
        mod(filter); 
        input.Customer = filter;
        return input;
    }

    public static OrderInput Address(this OrderInput input, Action<AddressInput?> mod)
    {
        var filter = new AddressInput?();
        mod ??= _ => { };
        mod(filter); 
        input.Address = filter;
        return input;
    }


    public static OrderInput OrderDate(this OrderInput input, DateTimeOffset val)
    {
         input.OrderDate = val;
         return input;
    }

    public static OrderInput Lines(this OrderInput input, Action<List<OrderLineInput>> mod)
    {
        var filter = new List<OrderLineInput>();
        mod ??= _ => { };
        mod(filter); 
        input.Lines = filter;
        return input;
    }


    public static OrderInput EntryTime(this OrderInput input, TimeSpan? val)
    {
         input.EntryTime = val;
         return input;
    }

}

public static class OrderLineInputExtensions
{ 
	
    public static OrderLineInput LineNumber(this OrderLineInput input, int val)
    {
         input.LineNumber = val;
         return input;
    }

    public static OrderLineInput Order(this OrderLineInput input, Action<OrderInput> mod)
    {
        var filter = new OrderInput();
        mod ??= _ => { };
        mod(filter); 
        input.Order = filter;
        return input;
    }

    public static OrderLineInput Item(this OrderLineInput input, Action<ItemInput?> mod)
    {
        var filter = new ItemInput?();
        mod ??= _ => { };
        mod(filter); 
        input.Item = filter;
        return input;
    }


    public static OrderLineInput Price(this OrderLineInput input, decimal val)
    {
         input.Price = val;
         return input;
    }


    public static OrderLineInput Quantity(this OrderLineInput input, double val)
    {
         input.Quantity = val;
         return input;
    }

}
