using System.Net;
using System.Net.Http.Headers;
using Linq2GraphQL.Client;
using Linq2GraphQL.TestClient;
using Nullable = Linq2GraphQL.TestClientNullable;

namespace Linq2GraphQL.Compat;

internal sealed record CompatScenario(string Name, Func<CompatContext, Task<object>> Run);

/// <summary>
/// What an app does with a generated client. The code compiles against both the baseline packages and this code, so
/// it can only use API both have; a scenario that needs newer API belongs in Linq2GraphQL.Tests.
/// </summary>
internal static class CompatScenarios
{
    private static readonly Guid CustomerId = Guid.Parse("0190a4c2-7b3e-7c1a-9d2f-3e4b5a6c7d8e");

    public static IReadOnlyList<CompatScenario> All { get; } =
    [
        Query("Hello_Default", c => c.Client().Query.Hello().Select()),
        Query("Hello_WithArgument", c => c.Client().Query.Hello("Magnus").Select()),
        Query("Hello_SafeMode", c => c.SafeModeClient().Query.Hello("Magnus").Select()),

        Query("Customers_Default", c => c.SafeModeClient().Query.Customers().Select()),
        Query("Customers_SafeModeOff", c => c.Client().Query.Customers().Select()),
        Query("Customers_IncludeOrdersAndLines", c => c.SafeModeClient().Query.Customers()
            .Include(e => e.Select(customer => customer.Orders.Select(order => order.Lines.Select(line => line.Item))))
            .Select()),
        Query("Customers_Projection", c => c.SafeModeClient().Query.Customers()
            .Select(e => e.Select(customer => new { customer.CustomerId, customer.CustomerName, customer.Status }))),
        Query("Customers_SelectMany", c => c.SafeModeClient().Query.Customers()
            .Select(e => e.SelectMany(customer => customer.Orders,
                (customer, order) => new { customer.CustomerName, order.OrderId }))),
        Query("Customers_ExtensionFieldWithArgument", c => c.SafeModeClient().Query.Customers()
            .Select(e => e.Select(customer => new
            {
                customer.CustomerName,
                Related = customer.RelatedCustomers(1).Select(related => related.CustomerName),
                First = customer.RelatedCustomer(2).CustomerName
            }))),
        Query("Customer_ReturnsNull", c => c.SafeModeClient().Query.CustomerReturnNull().Select()),

        Query("Orders_TopLevelArgument", c => c.SafeModeClient().Query.Orders(1).Select(e => e.Nodes)),
        Query("Orders_ArgumentOnNestedField", c => c.SafeModeClient().Query.Orders(2)
            .Select(e => e.Nodes.Select(order => new
            {
                order.OrderId, Invoice = order.OrderAddress(AddressType.Invoice)
            }))),
        Query("Orders_SameFieldWithDifferentArguments", c => c.SafeModeClient().Query.Orders()
            .Select(e => e.Nodes.Select(order => new
            {
                Jocke = order.OrderHello("Jocke", 22), Kalle = order.OrderHello("Kalle", 11)
            }))),
        Query("Orders_SameArgumentNameAtTwoLevels", c => c.SafeModeClient().Query.Orders(1)
            .Include(e => e.Nodes.Select(order => order.OrderHello("Jocke", 2)))
            .Include(e => e.Nodes.Select(order => order.OrderAddress(AddressType.Delivery)))
            .Select(e => e.Nodes)),
        Query("Orders_IncludeMultipleLevels", c => c.SafeModeClient().Query.Orders(1)
            .Include(e => e.Nodes.Select(order => order.Customer))
            .Include(e => e.Nodes.Select(order => order.Customer.Orders))
            .Select(e => e.Nodes)),
        Query("Orders_IncludeMultipleLevels_SafeModeOff", c => c.Client().Query.Orders(1)
            .Include(e => e.Nodes.Select(order => order.Customer))
            .Select(e => e.Nodes)),
        Query("Orders_LinqOperators", c => c.SafeModeClient().Query.Orders()
            .Select(e => e.Nodes
                .Where(order => order.OrderId != Guid.Empty)
                .Select(order => new { order.OrderId, order.Customer.CustomerName, Lines = order.Lines.Count() }))),
        Query("Orders_FilterAndSort", c => c.SafeModeClient().Query.Orders(first: 2,
                where: new OrderFilterInput
                {
                    Customer = new CustomerFilterInput
                    {
                        CustomerName = new StringOperationFilterInput { StartsWith = "Jocke" }
                    }
                },
                order: [new OrderSortInput { OrderDate = SortEnumType.Desc }])
            .Include(e => e.TotalCount)
            .Select(e => new { e.TotalCount, Orders = e.Nodes.Select(order => new { order.OrderId, order.OrderDate }) })),
        Query("Orders_NoTotalCount", c => c.SafeModeClient().Query.OrdersNoTotalCount(3).Select(e => e.Nodes)),
        Query("Animals_InterfaceSubtypes", c => c.SafeModeClient().Query.Animals()
            .Include(e => e.Nodes.Select(animal => animal.Pig()))
            .Include(e => e.Nodes.Select(animal => animal.Spider()))
            .Select(e => e.Nodes.Select(animal => new
            {
                animal.__TypeName, animal.Name, Pig = animal.Pig(), Spider = animal.Spider()
            }))),

        Query("Mutation_ScalarArgument", c => c.SafeModeClient().Mutation.SetName("Magnus").Select()),
        Query("Mutation_InputObject", c => c.SafeModeClient().Mutation.AddCustomer(new CustomerInput
            {
                CustomerId = CustomerId, CustomerName = "New Customer", Orders = [], Status = CustomerStatus.Active
            })
            .Select(e => new { e.CustomerId, e.CustomerName, e.Status })),

        Query("Errors_ResolverError", c => c.SafeModeClient().Query.RaiseError("boom").Select()),
        Query("Errors_Authentication", c => c.Client().Query.RaiseAuthError().Select()),
        new("Errors_UnsupportedExpression", c => Task.FromResult<object>(c.Client().Query.Orders()
            .Select(e => e.Nodes.Concat(e.Nodes)))),
        new("Errors_CancelledBeforeSending", async c =>
            await c.Client().Query.Hello().Select().ExecuteAsync(new CancellationToken(true))),

        Raw("Raw_Query", c => c.Client().GraphClient.ExecuteAsync("query { hello(name: \"Jocke\") }")),
        Raw("Raw_Variables", c => c.Client().GraphClient.ExecuteAsync("query($name: String) { hello(name: $name) }",
            new Dictionary<string, object> { ["name"] = "Magnus" })),
        Raw("Raw_InvalidDocument", c => c.Client().GraphClient.ExecuteAsync("query { doesNotExist }")),
        Raw("Raw_ResolverError", c => c.Client().GraphClient.ExecuteAsync("query { raiseError(message: \"boom\") }")),
        Raw("Raw_Mutation", c => c.Client().GraphClient.ExecuteAsync("mutation { setName(name: \"Raw\") }")),

        new("Request_Text", async c =>
        {
            var query = c.SafeModeClient().Query.Orders(1)
                .Include(e => e.Nodes.Select(order => order.OrderAddress(AddressType.Invoice)))
                .Select(e => e.Nodes);
            var request = await query.GetRequestAsync();
            return new { request.Query, request.Variables, Json = await query.GetRequestAsJsonAsync() };
        }),
        new("Request_ExecutedTwice", async c =>
        {
            var query = c.SafeModeClient().Query.Hello("Twice").Select();
            return new { First = await query.ExecuteAsync(), Second = await query.ExecuteAsync() };
        }),

        new("Paging_Pager", async c =>
        {
            var pager = c.SafeModeClient().Query.Orders(first: 1)
                .Include(e => e.TotalCount)
                .Select(e => e.Nodes.Select(order => new { order.OrderId }))
                .AsPager();
            var first = await pager.NextPageAsync();
            var totalCount = pager.PagerResult.TotalCount;
            var second = await pager.NextPageAsync();
            var back = await pager.PreviousPageAsync();
            return new { first, totalCount, second, back, pager.PagerResult.PageInfo };
        }),
        new("Paging_AllPages", async c =>
        {
            var pager = c.SafeModeClient().Query.Orders(first: 1)
                .Select(e => e.Nodes.Select(order => order.OrderId))
                .AsPager();
            var pages = new List<object>();
            await foreach (var page in pager.GetAllPagesWithResultAsync(maxPages: 3))
            {
                pages.Add(new { page.Data, page.Errors, page.HasErrors });
            }

            return pages;
        }),
        new("Paging_Manual", async c =>
        {
            var query = c.SafeModeClient().Query.OrdersNoBackwardPagination(2)
                .Include(e => e.PageInfo)
                .Include(e => e.TotalCount)
                .Select(e => e.Nodes.Select(order => new { order.OrderId, order.OrderDate }));
            await query.ExecuteBaseAsync();
            var first = query.ConvertResult(query.BaseResult);
            query.QueryNode.SetArgumentValue("after", query.BaseResult.PageInfo.EndCursor);
            var second = await query.ExecuteAsync();
            return new { first, second, query.BaseResult.TotalCount, query.BaseResult.PageInfo };
        }),

        new("Hooks_OptionsAndDerivedClients", async c =>
        {
            var client = c.Client(new GraphClientOptions
            {
                ConfigureRequest = (request, _) =>
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "app-token");
                    request.Headers.Add("X-Order", "options");
                    return ValueTask.CompletedTask;
                }
            });
            var derived = client
                .WithRequestConfiguration((request, _) =>
                {
                    request.Headers.Add("X-Order", "derived");
                    return ValueTask.CompletedTask;
                })
                .WithAccessToken(_ => ValueTask.FromResult("user-token"));
            return new
            {
                Original = await client.Query.Hello("original").Select().ExecuteAsync(),
                Derived = await derived.Query.Hello("derived").Select().ExecuteAsync(),
                Raw = (await derived.GraphClient.ExecuteAsync("query { hello }")).Data
            };
        }),
        new("Hooks_SafeModeSchemaRequest", async c =>
        {
            var client = c.SafeModeClient().WithAccessToken(_ => ValueTask.FromResult("user-token"));
            return await client.Query.Hello().Select().ExecuteAsync();
        }),
        new("DependencyInjection_RegisteredClient", async c =>
            await c.RegisteredClient().Query.Orders(1).Select(e => e.Nodes.Select(order => order.OrderId))
                .ExecuteAsync()),

        new("Subscription_Events", async c =>
        {
            var client = c.SafeModeClient();
            var observable = await client.Subscription.CustomerAdded()
                .Select(customer => new { customer.CustomerId, customer.CustomerName, customer.Status })
                .StartAsync();
            var received = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var subscription = observable.Subscribe(customer => received.TrySetResult(customer),
                exception => received.TrySetException(exception));
            return await RaiseUntilReceivedAsync(c, received.Task, "Subscribed");
        }),
        new("Subscription_EventsWithErrors", async c =>
        {
            var client = c.SafeModeClient();
            var observable = await client.Subscription.CustomerNameAdded("Topic Customer").Select().StartWithErrorsAsync();
            var received = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var subscription = observable.Subscribe(
                result => received.TrySetResult(new { result.Data, result.Errors, result.HasErrors }),
                exception => received.TrySetException(exception));
            return await RaiseUntilReceivedAsync(c, received.Task, "Topic Customer");
        }),

        Stub("Response_ServerErrorPlainText", HttpStatusCode.InternalServerError, "Something broke", "text/plain"),
        Stub("Response_UnauthorizedWithErrors", HttpStatusCode.Unauthorized,
            """{"errors":[{"message":"Not authenticated","extensions":{"code":"UNAUTHENTICATED"}}]}"""),
        Stub("Response_BadRequestWithErrors", HttpStatusCode.BadRequest,
            """{"errors":[{"message":"Unknown field","locations":[{"line":1,"column":9}]}]}"""),
        Stub("Response_EmptyBody", HttpStatusCode.OK, ""),
        Stub("Response_NotJson", HttpStatusCode.OK, "<html>Login</html>", "text/html"),
        Stub("Response_NoDataNoErrors", HttpStatusCode.OK, """{"extensions":{"traceId":"abc"}}"""),
        Stub("Response_DataNullWithFieldError", HttpStatusCode.OK,
            """{"data":null,"errors":[{"message":"Field failed","path":["hello"],"extensions":{"code":"BAD_USER_INPUT"}}]}"""),
        Stub("Response_PartialData", HttpStatusCode.OK,
            """{"data":{"hello":null},"errors":[{"message":"Field failed","path":["hello"]}]}"""),
        Stub("Response_WithExtensions", HttpStatusCode.OK,
            """{"data":{"hello":"Hi"},"extensions":{"cost":{"requestedQueryCost":1},"tags":["a","b"]}}"""),
        Stub("Response_SiblingRootFields", HttpStatusCode.OK,
            """{"extensions":{"first":true},"data":{"other":{"deep":[1,2,{"x":null}]},"hello":"Hi","last":0}}"""),
        Stub("Response_ErrorsBeforeData", HttpStatusCode.OK,
            """{"errors":[{"message":"Warning","path":["other"]}],"data":{"hello":"Hi"}}"""),
        Stub("Response_EscapedStrings", HttpStatusCode.OK,
            """{"data":{"hello":"Line\nbreak \"quoted\" åäö 😀"}}"""),
        Stub("Response_TruncatedJson", HttpStatusCode.OK, """{"data":{"hello":"Hi"""),

        Query("Nullable_CustomerList", c => c.NullableClient().Query.CustomerList().Select()),
        Query("Nullable_ItemWithBytes", c => c.NullableClient().Query.Item().Include(e => e.Data).Select()),
        Query("Nullable_ObsoleteField", c => c.NullableClient().Query.ItemDraft().Select()),
        Query("Nullable_NullObject", c => c.NullableClient().Query.CustomerNullable().Select()),
        Query("Nullable_NullList", c => c.NullableClient().Query.CustomerListAllNullable().Select()),
        Query("Nullable_NullListOfNonNull", c => c.NullableClient().Query.CustomerListNullable().Select()),
        Query("Nullable_ListInList", c => c.NullableClient().Query.CustomerListInList().Select()),
        Query("Nullable_CustomScalars", c => c.NullableClient().Query.Person().Select()),
        Query("Nullable_CustomScalarInput", c => c.NullableClient().Mutation.UpdatePerson(new Nullable.PersonInput
        {
            Name = "Peter",
            MacAddress = new Nullable.MacAddress("01-23-45-67-89-ab"),
            Longitude = new Nullable.Longitude { Value = "14° 24' 0\" E" }
        }).Select()),
        Query("Nullable_InputObject", c => c.NullableClient().Mutation.UpdateCustomer(new Nullable.CustomerInput
        {
            CustomerId = CustomerId,
            CustomerName = "Updated",
            Status = Nullable.CustomerStatus.Disabled,
            Orders = [],
            Address = new Nullable.AddressInput
            {
                Name = "Home", Street = "Street 1", PostalCode = "12345", CreatedDate = new DateOnly(2024, 2, 29)
            },
            Created = new DateTimeOffset(2024, 2, 29, 23, 59, 58, TimeSpan.FromHours(2))
        }).Select(e => new { e.CustomerId, e.CustomerName, e.Status, e.Address, e.Created })),
    ];

    /// <summary>Runs the query with ExecuteWithResultAsync and then ExecuteAsync, which can throw.</summary>
    private static CompatScenario Query<T, TResult>(string name,
        Func<CompatContext, GraphQueryExecute<T, TResult>> createQuery)
    {
        return new CompatScenario(name, async context =>
        {
            var query = createQuery(context);
            var withResult = await query.ExecuteWithResultAsync();
            return new
            {
                WithResult = new { withResult.Data, withResult.Errors, withResult.Extensions, withResult.HasErrors },
                Execute = await CompatRunner.AttemptAsync(async () => await query.ExecuteAsync())
            };
        });
    }

    private static CompatScenario Raw(string name, Func<CompatContext, Task<GraphResult<System.Text.Json.JsonElement>>> execute)
    {
        return new CompatScenario(name, async context =>
        {
            var result = await execute(context);
            return new { result.Data, result.Errors, result.Extensions, result.HasErrors, result.HasData };
        });
    }

    private static CompatScenario Stub(string name, HttpStatusCode status, string body,
        string mediaType = "application/json")
    {
        return new CompatScenario(name, async context =>
        {
            var query = context.StubClient(status, body, mediaType).Query.Hello().Select();
            return new
            {
                WithResult = await CompatRunner.AttemptAsync(async () =>
                {
                    var result = await query.ExecuteWithResultAsync();
                    return new { result.Data, result.Errors, result.Extensions, result.HasErrors };
                }),
                Execute = await CompatRunner.AttemptAsync(async () => await query.ExecuteAsync()),
                Raw = await CompatRunner.AttemptAsync(async () =>
                {
                    var result = await context.StubClient(status, body, mediaType).GraphClient
                        .ExecuteAsync("query { hello }");
                    return new { result.Data, result.Errors, result.Extensions, result.HasErrors };
                })
            };
        });
    }

    /// <summary>
    /// Raises the event until the subscription delivers it: a subscription has no public signal for when the server
    /// has registered it, and an event raised before that is lost.
    /// </summary>
    private static async Task<object> RaiseUntilReceivedAsync(CompatContext context, Task<object> received,
        string customerName)
    {
        var mutation = $$"""
            mutation { addCustomer(customer: { customerId: "{{CustomerId}}", customerName: "{{customerName}}", status: ACTIVE, orders: [] }) { customerId } }
            """;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (!received.IsCompleted)
        {
            timeout.Token.ThrowIfCancellationRequested();
            await context.SendUnrecordedAsync(mutation);
            await Task.WhenAny(received, Task.Delay(100, timeout.Token));
        }

        return await received;
    }
}
