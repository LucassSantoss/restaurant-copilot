using System.Reflection;
using NetArchTest.Rules;
using ArchTestResult = NetArchTest.Rules.TestResult;

namespace RestaurantCopilot.Api.Tests.Architecture;

/// <summary>
/// Enforces the Clean Architecture dependency rule: dependencies only point inward
/// (Api -> Infrastructure -> Application -> Domain). These tests fail the build as soon
/// as a layer starts depending on something it should not know about.
/// </summary>
public sealed class LayerDependencyTests
{
    private const string DomainNamespace = "RestaurantCopilot.Domain";
    private const string ApplicationNamespace = "RestaurantCopilot.Application";
    private const string InfrastructureNamespace = "RestaurantCopilot.Infrastructure";
    private const string ApiNamespace = "RestaurantCopilot.Api";

    private static readonly Assembly DomainAssembly = Assembly.Load(DomainNamespace);
    private static readonly Assembly ApplicationAssembly = Assembly.Load(ApplicationNamespace);
    private static readonly Assembly InfrastructureAssembly = Assembly.Load(InfrastructureNamespace);

    [Fact]
    public void Domain_DoesNotDependOnOuterLayers()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(ApplicationNamespace, InfrastructureNamespace, ApiNamespace)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Describe(result));
    }

    [Fact]
    public void Application_DoesNotDependOnInfrastructureOrApi()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(InfrastructureNamespace, ApiNamespace)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Describe(result));
    }

    [Fact]
    public void Application_DoesNotDependOnConcreteTechnologies()
    {
        // Application talks to the outside world only through its own abstractions (ports).
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Npgsql",
                "Pgvector",
                "StackExchange.Redis",
                "Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Describe(result));
    }

    [Fact]
    public void Infrastructure_DoesNotDependOnApi()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Describe(result));
    }

    private static string Describe(ArchTestResult result) =>
        $"Offending types: {string.Join(", ", result.FailingTypeNames ?? [])}";
}
