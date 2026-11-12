using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using Simulation.Checkout.Application;
using Simulation.Checkout.Application.Abstractions;
using Simulation.Checkout.Domain;
using Simulation.Checkout.Infrastructure;

namespace Simulation.Checkout.Tests;

public class ArchitectureTests
{
  private static readonly Assembly _domainAssembly = typeof(Order).Assembly;
  private static readonly Assembly _applicationAssembly = typeof(CheckoutService).Assembly;
  private static readonly string _applicationNameSpace = typeof(CheckoutService).Namespace!;
  private static readonly Assembly _infrastructureAssembly = typeof(InMemoryOrderRepository).Assembly;
  private static readonly string _infrastructureNamespace = typeof(InMemoryOrderRepository).Namespace!;

  [Fact]
  public void Domain_WhenCheckedForDependencies_ShouldNotDependOnApplicationOrInfrastructure()
  {
    // Arrange
    var type = Types.InAssembly(_domainAssembly);
    
    
    // Act
    var result = type
    .ShouldNot()
    .HaveDependencyOnAny([_applicationNameSpace, _infrastructureNamespace])
    .GetResult();

    // Assert
    result.IsSuccessful.Should().BeTrue();
  }

  [Fact]
  public void Application_WhenCheckedForDependencies_ShouldNotDependOnInfrastructure()
  {
    // Arrange
    var type = Types.InAssembly(_applicationAssembly);

    // Act
    var result = type
    .ShouldNot()
    .HaveDependencyOn(_infrastructureNamespace)
    .GetResult();

    // Assert
    result.IsSuccessful.Should().BeTrue();
  }

  [Fact]
  public void Interfaces_WhenDefined_ShouldStartWithI()
  {
    // Arrange
    var types = Types.InAssemblies([_domainAssembly, _applicationAssembly, _infrastructureAssembly]);

    // Act
    var result = types
    .That()
    .AreInterfaces()
    .Should()
    .HaveNameStartingWith("I")
    .GetResult();

    // Assert
    result.IsSuccessful.Should().BeTrue();
  }

  [Fact]
  public void Classes_WhenImplementingIOrderRepository_ShouldEndWithRepository()
  {
    // Arrange
    var types = Types.InAssemblies([_domainAssembly, _applicationAssembly, _infrastructureAssembly]);

    // Act
    var result = types
    .That()
    .AreClasses()
    .And()
    .ImplementInterface(typeof(IOrderRepository))
    .Should()
    .HaveNameEndingWith("Repository")
    .GetResult();

    // Assert
    result.IsSuccessful.Should().BeTrue();
  }
}