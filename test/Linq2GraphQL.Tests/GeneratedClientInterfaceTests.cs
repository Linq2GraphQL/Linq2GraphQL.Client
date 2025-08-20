using System.Reflection;
using Linq2GraphQL.TestClient;
using Shouldly;

namespace Linq2GraphQL.Tests;

public class GeneratedClientInterfaceTests
{
    [Fact]
    public void SampleClient_ShouldImplement_ISampleClient()
    {
        // Arrange & Act
        var client = typeof(SampleClient);
        var interfaceType = typeof(ISampleClient);

        // Assert
        interfaceType.IsAssignableFrom(client).ShouldBeTrue();
    }

    [Fact]
    public void QueryMethods_ShouldImplement_IQueryMethods()
    {
        // Arrange & Act
        var queryMethods = typeof(QueryMethods);
        var interfaceType = typeof(IQueryMethods);

        // Assert
        interfaceType.IsAssignableFrom(queryMethods).ShouldBeTrue();
    }

    [Fact]
    public void MutationMethods_ShouldImplement_IMutationMethods()
    {
        // Arrange & Act
        var mutationMethods = typeof(MutationMethods);
        var interfaceType = typeof(IMutationMethods);

        // Assert
        interfaceType.IsAssignableFrom(mutationMethods).ShouldBeTrue();
    }

    [Fact]
    public void SubscriptionMethods_ShouldImplement_ISubscriptionMethods()
    {
        // Arrange & Act
        var subscriptionMethods = typeof(SubscriptionMethods);
        var interfaceType = typeof(ISubscriptionMethods);

        // Assert
        interfaceType.IsAssignableFrom(subscriptionMethods).ShouldBeTrue();
    }

    [Fact]
    public void SampleClient_Properties_ShouldBeInterfaceTypes()
    {
        // Arrange
        var clientType = typeof(SampleClient);
        
        // Act
        var queryProperty = clientType.GetProperty("Query");
        var mutationProperty = clientType.GetProperty("Mutation");
        var subscriptionProperty = clientType.GetProperty("Subscription");

        // Assert
        queryProperty.ShouldNotBeNull();
        queryProperty.PropertyType.ShouldBe(typeof(IQueryMethods));
        
        mutationProperty.ShouldNotBeNull();
        mutationProperty.PropertyType.ShouldBe(typeof(IMutationMethods));
        
        subscriptionProperty.ShouldNotBeNull();
        subscriptionProperty.PropertyType.ShouldBe(typeof(ISubscriptionMethods));
    }

    [Fact]
    public void Interface_ShouldExposeAllPublicMethods()
    {
        // Arrange
        var queryMethodsType = typeof(QueryMethods);
        var iQueryMethodsType = typeof(IQueryMethods);
        
        // Act
        var concreteMethods = GetPublicInstanceMethods(queryMethodsType);
        var interfaceMethods = iQueryMethodsType.GetMethods()
            .Select(m => m.Name)
            .OrderBy(n => n)
            .ToList();

        // Assert
        concreteMethods.ShouldBe(interfaceMethods);
    }

    private static List<string> GetPublicInstanceMethods(Type type)
    {
        return type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName) // Exclude properties
            .Select(m => m.Name)
            .OrderBy(n => n)
            .ToList();
    }
}
