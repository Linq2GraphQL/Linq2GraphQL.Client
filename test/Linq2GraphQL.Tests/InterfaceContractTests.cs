using System.Reflection;
using Linq2GraphQL.TestClient;
using Shouldly;

namespace Linq2GraphQL.Tests;

public class InterfaceContractTests
{
    [Fact]
    public void IQueryMethods_ShouldExposeAllPublicMethodsFromQueryMethods()
    {
        // Arrange
        var concreteType = typeof(QueryMethods);
        var interfaceType = typeof(IQueryMethods);

        // Act
        var concreteMethods = GetPublicInstanceMethods(concreteType);
        var interfaceMethods = GetInterfaceMethods(interfaceType);

        // Assert
        interfaceMethods.ShouldBeSubsetOf(concreteMethods);
        concreteMethods.ShouldBeSubsetOf(interfaceMethods);
    }

    [Fact]
    public void IMutationMethods_ShouldExposeAllPublicMethodsFromMutationMethods()
    {
        // Arrange
        var concreteType = typeof(MutationMethods);
        var interfaceType = typeof(IMutationMethods);

        // Act
        var concreteMethods = GetPublicInstanceMethods(concreteType);
        var interfaceMethods = GetInterfaceMethods(interfaceType);

        // Assert
        interfaceMethods.ShouldBeSubsetOf(concreteMethods);
        concreteMethods.ShouldBeSubsetOf(interfaceMethods);
    }

    [Fact]
    public void ISubscriptionMethods_ShouldExposeAllPublicMethodsFromSubscriptionMethods()
    {
        // Arrange
        var concreteType = typeof(SubscriptionMethods);
        var interfaceType = typeof(ISubscriptionMethods);

        // Act
        var concreteMethods = GetPublicInstanceMethods(concreteType);
        var interfaceMethods = GetInterfaceMethods(interfaceType);

        // Assert
        interfaceMethods.ShouldBeSubsetOf(concreteMethods);
        concreteMethods.ShouldBeSubsetOf(interfaceMethods);
    }

    [Fact]
    public void ISampleClient_ShouldExposeAllPublicPropertiesFromSampleClient()
    {
        // Arrange
        var concreteType = typeof(SampleClient);
        var interfaceType = typeof(ISampleClient);

        // Act
        var concreteProperties = GetPublicProperties(concreteType);
        var interfaceProperties = GetInterfaceProperties(interfaceType);

        // Assert
        interfaceProperties.ShouldBeSubsetOf(concreteProperties);
        concreteProperties.ShouldBeSubsetOf(interfaceProperties);
    }

    [Fact]
    public void Interface_MethodSignatures_ShouldMatchConcreteImplementation()
    {
        // Arrange
        var concreteType = typeof(QueryMethods);
        var interfaceType = typeof(IQueryMethods);

        // Act & Assert
        foreach (var interfaceMethod in interfaceType.GetMethods())
        {
            var concreteMethod = concreteType.GetMethod(interfaceMethod.Name, 
                BindingFlags.Public | BindingFlags.Instance);

            concreteMethod.ShouldNotBeNull($"Method {interfaceMethod.Name} should exist in concrete class");

            // Verify parameter types match
            var interfaceParams = interfaceMethod.GetParameters();
            var concreteParams = concreteMethod.GetParameters();

            interfaceParams.Length.ShouldBe(concreteParams.Length);

            for (int i = 0; i < interfaceParams.Length; i++)
            {
                interfaceParams[i].ParameterType.ShouldBe(concreteParams[i].ParameterType);
                interfaceParams[i].Name.ShouldBe(concreteParams[i].Name);
            }

            // Verify return type matches
            interfaceMethod.ReturnType.ShouldBe(concreteMethod.ReturnType);
        }
    }

    [Fact]
    public void Interface_PropertySignatures_ShouldMatchConcreteImplementation()
    {
        // Arrange
        var concreteType = typeof(SampleClient);
        var interfaceType = typeof(ISampleClient);

        // Act & Assert
        foreach (var interfaceProperty in interfaceType.GetProperties())
        {
            var concreteProperty = concreteType.GetProperty(interfaceProperty.Name);

            concreteProperty.ShouldNotBeNull($"Property {interfaceProperty.Name} should exist in concrete class");

            // Verify property type matches
            interfaceProperty.PropertyType.ShouldBe(concreteProperty.PropertyType);

            // Verify getter/setter accessibility
            if (interfaceProperty.CanRead)
            {
                concreteProperty.CanRead.ShouldBeTrue($"Property {interfaceProperty.Name} should be readable");
            }

            if (interfaceProperty.CanWrite)
            {
                concreteProperty.CanWrite.ShouldBeTrue($"Property {interfaceProperty.Name} should be writable");
            }
        }
    }

    private static List<string> GetPublicInstanceMethods(Type type)
    {
        return type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName) // Exclude properties
            .Select(m => m.Name)
            .OrderBy(n => n)
            .ToList();
    }

    private static List<string> GetInterfaceMethods(Type type)
    {
        return type.GetMethods()
            .Select(m => m.Name)
            .OrderBy(n => n)
            .ToList();
    }

    private static List<string> GetPublicProperties(Type type)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .OrderBy(n => n)
            .ToList();
    }

    private static List<string> GetInterfaceProperties(Type type)
    {
        return type.GetProperties()
            .Select(p => p.Name)
            .OrderBy(n => n)
            .ToList();
    }
}
