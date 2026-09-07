using System.Linq.Expressions;
using System.Text;
using Linq2GraphQL.Client;
using Linq2GraphQL.TestClient;
using Shouldly;

namespace Linq2GraphQL.Tests;

/// <summary>
///     Covers the expression parser on its own: an expression goes in, the selection tree that the query text is
///     generated from comes out. No server and no schema is involved.
/// </summary>
public class ExpressionParserTests
{
    /// <summary>
    ///     Parses <paramref name="path" /> against a root node of <typeparamref name="T" /> and asserts the
    ///     fields it selects, one dotted path per line, with a trailing <c>*</c> on the fields whose primitive
    ///     properties are also selected.
    /// </summary>
    private static void ShouldSelect<T, TProperty>(Expression<Func<T, TProperty>> path, string expected)
    {
        Parse(path).ShouldBe(expected.ReplaceLineEndings("\n").Trim());
    }

    private static string Parse<T, TProperty>(Expression<Func<T, TProperty>> path)
    {
        var root = new QueryNode(typeof(T), "root", null, null, true);
        Utilities.ParseExpression(path, root);

        var builder = new StringBuilder();
        Describe(root, "", builder);
        return builder.ToString().TrimEnd('\n');
    }

    private static void Describe(QueryNode node, string prefix, StringBuilder builder)
    {
        foreach (var child in node.ChildNodes)
        {
            var path = prefix + (child.Alias ?? child.Name);
            builder.Append(path).Append(child.IncludePrimitive ? "*\n" : "\n");
            Describe(child, path + ".", builder);
        }
    }

    [Fact]
    public void MemberPath()
    {
        ShouldSelect((OrdersConnection e) => e.Nodes, "nodes*");
    }

    [Fact]
    public void MemberChain_OnlyTheLastFieldTakesPrimitives()
    {
        ShouldSelect((Order e) => e.Customer.Orders,
            """
            customer
            customer.orders*
            """);
    }

    [Fact]
    public void Select_ProjectionMovesTheSelection()
    {
        ShouldSelect((OrdersConnection e) => e.Nodes.Select(n => n.Customer),
            """
            nodes
            nodes.customer*
            """);
    }

    [Fact]
    public void Select_Nested()
    {
        ShouldSelect((OrdersConnection e) => e.Nodes.Select(n => n.Customer.Orders.Select(o => o.Address)),
            """
            nodes
            nodes.customer
            nodes.customer.orders
            nodes.customer.orders.address*
            """);
    }

    [Fact]
    public void Select_AnonymousType()
    {
        ShouldSelect((OrdersConnection e) => e.Nodes.Select(n => new { n.OrderId, n.Address }),
            """
            nodes
            nodes.orderId*
            nodes.address*
            """);
    }

    [Fact]
    public void Select_MemberInit()
    {
        ShouldSelect((OrdersConnection e) => e.Nodes.Select(n => new OrderIdAddress
            {
                OrderId = n.OrderId,
                Address = n.Address
            }),
            """
            nodes
            nodes.orderId*
            nodes.address*
            """);
    }

    [Fact]
    public void Select_NestedAnonymousTypes()
    {
        ShouldSelect((OrdersConnection e) => e.Nodes.Select(n => new
            {
                n.OrderId,
                Cust = new { n.Customer.CustomerName, n.Customer.Orders }
            }),
            """
            nodes
            nodes.orderId*
            nodes.customer
            nodes.customer.customerName*
            nodes.customer.orders*
            """);
    }

    [Fact]
    public void Parameter_SelectsTheWholeElement()
    {
        // "e" on its own means every primitive field of the root.
        ShouldSelect((OrdersConnection e) => new { All = e, Nodes = e.Nodes }, "nodes*");
    }

    [Fact]
    public void SelectMany()
    {
        ShouldSelect((List<Customer> e) => e.SelectMany(c => c.Orders), "orders*");
    }

    [Fact]
    public void SelectMany_WithResultSelector()
    {
        ShouldSelect((List<Customer> e) => e.SelectMany(c => c.Orders, (c, o) => new { c.CustomerName, o.OrderId }),
            """
            orders
            orders.orderId*
            customerName*
            """);
    }

    [Fact]
    public void Duplicated_PathsMerge()
    {
        ShouldSelect((OrdersConnection e) => new { e.Nodes, Ids = e.Nodes.Select(n => n.OrderId) },
            """
            nodes*
            nodes.orderId*
            """);
    }

    [Fact]
    public void GraphMethod_WithArgumentsIsAliased()
    {
        var tree = Parse((OrdersConnection e) => e.Nodes.Select(n => n.OrderAddress(AddressType.Delivery)));

        tree.ShouldStartWith("nodes\nnodes.orderAddress_");
        tree.ShouldEndWith("*");
    }

    [Fact]
    public void GraphMethod_SameFieldWithDifferentArgumentsBecomesTwoSelections()
    {
        var tree = Parse((OrdersConnection e) => e.Nodes.Select(n => new
        {
            Delivery = n.OrderAddress(AddressType.Delivery),
            Invoice = n.OrderAddress(AddressType.Invoice)
        }));

        tree.Split('\n').Length.ShouldBe(3);
        tree.ShouldNotContain("orderAddress*");
    }

    [Fact]
    public void GraphMethod_ArgumentsAreEvaluated()
    {
        var name = "Peter";
        var root = new QueryNode(typeof(OrdersConnection), "root", null, null, true);

        Utilities.ParseExpression((Expression<Func<OrdersConnection, object>>)(e =>
            e.Nodes.Select(n => n.OrderHello(name, 40 + 2))), root);

        var arguments = root.ChildNodes.Single().ChildNodes.Single().Arguments;

        arguments.Select(e => e.GraphName).ShouldBe(["name", "first"]);
        arguments.Select(e => e.Value).ShouldBe(["Peter", 42]);
    }

    // The shapes below were either silently dropped or threw before the parser was rewritten.

    [Fact]
    public void PassThroughOperator_WithoutLambda()
    {
        ShouldSelect((OrdersConnection e) => e.Nodes.First(), "nodes*");
        ShouldSelect((OrdersConnection e) => e.Nodes.Count(), "nodes*");
        ShouldSelect((OrdersConnection e) => e.Nodes.ToList(), "nodes*");
    }

    [Fact]
    public void PassThroughOperator_KeepsTheSelectionOnItsSource()
    {
        ShouldSelect((OrdersConnection e) => e.Nodes.Take(2).Select(n => n.OrderId),
            """
            nodes
            nodes.orderId*
            """);
    }

    [Fact]
    public void PassThroughOperator_FetchesTheFieldsItsKeySelectorReads()
    {
        // orderDate has to be in the response for the client side ordering to work.
        ShouldSelect((OrdersConnection e) => e.Nodes.OrderBy(n => n.OrderDate).Select(n => n.OrderId),
            """
            nodes
            nodes.orderDate*
            nodes.orderId*
            """);
    }

    [Fact]
    public void PassThroughOperator_FetchesTheFieldsItsPredicateReads()
    {
        ShouldSelect(
            (OrdersConnection e) => e.Nodes.Where(n => n.Customer.CustomerName == "Kalle").Select(n => n.OrderId),
            """
            nodes
            nodes.customer
            nodes.customer.customerName*
            nodes.orderId*
            """);
    }

    [Fact]
    public void ChainedOperators()
    {
        ShouldSelect((OrdersConnection e) => e.Nodes
                .Where(n => n.OrderId != Guid.Empty)
                .OrderByDescending(n => n.OrderDate)
                .Select(n => n.Customer)
                .Select(c => c.CustomerName),
            """
            nodes
            nodes.orderId*
            nodes.orderDate*
            nodes.customer
            nodes.customer.customerName*
            """);
    }

    [Fact]
    public void Cast_IsTransparent()
    {
        ShouldSelect((Order e) => (object)e.Customer, "customer*");
        ShouldSelect<Customer, Guid?>(e => e.CustomerId, "customerId*");
    }

    [Fact]
    public void NonGraphMemberOnAGraphField_SelectsTheField()
    {
        ShouldSelect((Order e) => e.EntryTime!.Value.Ticks, "entryTime*");
    }

    [Fact]
    public void OrdinaryMethodCall_FetchesWhatItReads()
    {
        ShouldSelect((Customer e) => e.CustomerName.ToUpper(), "customerName*");
    }

    [Fact]
    public void CapturedValue_SelectsNothing()
    {
        var captured = new Order { OrderId = Guid.NewGuid() };
        ShouldSelect((OrdersConnection e) => new { e.TotalCount, Captured = captured.OrderId }, "totalCount*");
    }

    [Fact]
    public void UnsupportedOperator_IsReported()
    {
        var exception = Should.Throw<NotSupportedException>(() =>
            Parse((OrdersConnection e) => e.Nodes.GroupBy(n => n.OrderId)));

        exception.Message.ShouldContain("GroupBy");
    }

    [Fact]
    public void UnboundParameter_IsReported()
    {
        var stray = Expression.Parameter(typeof(Order), "stray");
        var lambda = Expression.Lambda<Func<OrdersConnection, object>>(
            Expression.Lambda<Func<Order, object>>(
                Expression.Property(stray, nameof(Order.Address)), stray),
            Expression.Parameter(typeof(OrdersConnection), "e"));

        var root = new QueryNode(typeof(OrdersConnection), "root", null, null, true);

        var exception = Should.Throw<NotSupportedException>(() => Utilities.ParseExpression(lambda, root));
        exception.Message.ShouldContain("stray");
    }
}

public class ArgumentIdTests
{
    [Fact]
    public void EqualValuesGiveEqualIds()
    {
        // The alias is recomputed from the argument values when the response is read back, so it has to be a
        // pure function of those values.
        Utilities.GetArgumentsId(["Peter", 1234])
            .ShouldBe(Utilities.GetArgumentsId([new string("Peter".ToCharArray()), 1234]));
    }

    [Fact]
    public void DifferentValuesGiveDifferentIds()
    {
        Utilities.GetArgumentsId(["Peter", 1234])
            .ShouldNotBe(Utilities.GetArgumentsId(["Peter", 1235]));

        Utilities.GetArgumentsId([AddressType.Delivery])
            .ShouldNotBe(Utilities.GetArgumentsId([AddressType.Invoice]));
    }

    [Fact]
    public void ValuesAreSeparated()
    {
        Utilities.GetArgumentsId(["a", "bc"])
            .ShouldNotBe(Utilities.GetArgumentsId(["ab", "c"]));
    }

    [Fact]
    public void NoArgumentsGivesNoId()
    {
        Utilities.GetArgumentsId(null).ShouldBeNull();
        Utilities.GetArgumentsId([]).ShouldBeNull();
        Utilities.GetArgumentsId([null, null]).ShouldBeNull();
    }

    [Fact]
    public void IdIsStableBetweenProcesses()
    {
        // Hard coded on purpose: a reference based hash would make the generated query text differ from run to
        // run, which stops it from being cached.
        Utilities.GetArgumentsId(["Peter", 1234]).ShouldBe("_428b1fe100eabf57");
    }

    [Fact]
    public void ObjectsAreComparedByTheirContents()
    {
        var one = new AddressInput { Street = "Main", Name = "Gothenburg" };
        var other = new AddressInput { Street = "Main", Name = "Gothenburg" };
        var different = new AddressInput { Street = "Main", Name = "Stockholm" };

        Utilities.GetArgumentsId([one]).ShouldBe(Utilities.GetArgumentsId([other]));
        Utilities.GetArgumentsId([one]).ShouldNotBe(Utilities.GetArgumentsId([different]));
    }
}
