using Linq2GraphQL.Client;
using Linq2GraphQL.Client.Subscriptions;
using Linq2GraphQL.TestClient;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;

namespace Linq2GraphQL.Tests;

public class ClientMockingTests
{
    [Fact]
    public void Mock_ISampleClient_ShouldAllowMocking()
    {
        // Arrange
        var mockClient = new Mock<ISampleClient>();
        var mockQuery = new Mock<IQueryMethods>();
        var mockMutation = new Mock<IMutationMethods>();
        var mockSubscription = new Mock<ISubscriptionMethods>();

        mockClient.Setup(c => c.Query).Returns(mockQuery.Object);
        mockClient.Setup(c => c.Mutation).Returns(mockMutation.Object);
        mockClient.Setup(c => c.Subscription).Returns(mockSubscription.Object);

        // Act
        var query = mockClient.Object.Query;
        var mutation = mockClient.Object.Mutation;
        var subscription = mockClient.Object.Subscription;

        // Assert
        query.ShouldBe(mockQuery.Object);
        mutation.ShouldBe(mockMutation.Object);
        subscription.ShouldBe(mockSubscription.Object);
    }

    [Fact]
    public void Mock_IQueryMethods_ShouldAllowMockingQueryExecution()
    {
        // Arrange
        var mockQueryMethods = new Mock<IQueryMethods>();
        mockQueryMethods.Setup(q => q.Hello("Test"))
            .Returns(new GraphQuery<string>(null, "hello", OperationType.Query, new List<ArgumentValue>()));

        // Act
        var result = mockQueryMethods.Object.Hello("Test");

        // Assert
        result.ShouldNotBeNull();
        mockQueryMethods.Verify(q => q.Hello("Test"), Times.Once);
    }

    [Fact]
    public void Mock_IMutationMethods_ShouldAllowMockingMutationExecution()
    {
        // Arrange
        var mockMutationMethods = new Mock<IMutationMethods>();
        var customerInput = new CustomerInput
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "Test Customer",
            Status = CustomerStatus.Active,
            Orders = new List<OrderInput>()
        };

        mockMutationMethods.Setup(m => m.AddCustomer(customerInput))
            .Returns(new GraphQuery<Customer>(null, "addCustomer", OperationType.Mutation, new List<ArgumentValue>()));

        // Act
        var result = mockMutationMethods.Object.AddCustomer(customerInput);

        // Assert
        result.ShouldNotBeNull();
        mockMutationMethods.Verify(m => m.AddCustomer(customerInput), Times.Once);
    }

    [Fact]
    public void Mock_ISubscriptionMethods_ShouldAllowMockingSubscription()
    {
        // Arrange
        var mockSubscriptionMethods = new Mock<ISubscriptionMethods>();
        
        mockSubscriptionMethods.Setup(s => s.CustomerAdded())
            .Returns(new GraphSubscription<Customer>(null, "customerAdded", OperationType.Subscription, new List<ArgumentValue>()));

        // Act
        var result = mockSubscriptionMethods.Object.CustomerAdded();

        // Assert
        result.ShouldNotBeNull();
        mockSubscriptionMethods.Verify(s => s.CustomerAdded(), Times.Once);
    }

    [Fact]
    public void DependencyInjection_WithMockedInterfaces_ShouldWork()
    {
        // Arrange
        var mockQueryMethods = new Mock<IQueryMethods>();
        var mockMutationMethods = new Mock<IMutationMethods>();
        var mockSubscriptionMethods = new Mock<ISubscriptionMethods>();

        // Act & Assert - This test verifies that interfaces can be mocked
        // demonstrating the flexibility of interfaces for testing
        mockQueryMethods.ShouldNotBeNull();
        mockMutationMethods.ShouldNotBeNull();
        mockSubscriptionMethods.ShouldNotBeNull();
        
        // Verify that mocked interfaces can be used
        mockQueryMethods.Setup(q => q.Hello("Test"))
            .Returns(new GraphQuery<string>(null, "hello", OperationType.Query, new List<ArgumentValue>()));
        
        var result = mockQueryMethods.Object.Hello("Test");
        result.ShouldNotBeNull();
    }

    [Fact]
    public void Interface_Contract_ShouldMatchConcreteImplementation()
    {
        // Arrange
        var concreteClient = typeof(SampleClient);
        var interfaceClient = typeof(ISampleClient);

        // Act
        var concreteProperties = concreteClient.GetProperties()
            .Where(p => p.Name == "Query" || p.Name == "Mutation" || p.Name == "Subscription")
            .OrderBy(p => p.Name)
            .ToList();

        var interfaceProperties = interfaceClient.GetProperties()
            .OrderBy(p => p.Name)
            .ToList();

        // Assert
        concreteProperties.Count.ShouldBe(interfaceProperties.Count);
        
        for (int i = 0; i < concreteProperties.Count; i++)
        {
            concreteProperties[i].Name.ShouldBe(interfaceProperties[i].Name);
            concreteProperties[i].PropertyType.ShouldBe(interfaceProperties[i].PropertyType);
        }
    }
}
