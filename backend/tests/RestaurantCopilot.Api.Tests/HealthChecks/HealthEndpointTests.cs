using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using RestaurantCopilot.Api.Tests.Infrastructure;

namespace RestaurantCopilot.Api.Tests.HealthChecks;

[Collection(ApiTestCollectionDefinition.Name)]
public sealed class HealthEndpointTests(ApiFactory factory)
{
    [Fact]
    public async Task Get_WhenAllDependenciesAreUp_ReturnsHealthyWithEveryCheck()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<HealthBody>(TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.ShouldNotBeNull();
        body.Status.ShouldBe("Healthy");
        body.Checks.Keys.ShouldBe(["postgres", "redis"], ignoreOrder: true);
    }

    [Fact]
    public async Task Get_WhenRedisIsUnreachable_ReturnsServiceUnavailable()
    {
        await using var brokenFactory = factory.WithWebHostBuilder(builder =>
            builder.UseSetting("ConnectionStrings:Redis", "localhost:1,connectTimeout=500,abortConnect=false"));
        using var client = brokenFactory.CreateClient();

        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<HealthBody>(TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        body.ShouldNotBeNull();
        body.Checks["redis"].Status.ShouldBe("Unhealthy");
        body.Checks["postgres"].Status.ShouldBe("Healthy");
    }

    private sealed record HealthBody(string Status, Dictionary<string, HealthCheckBody> Checks);

    private sealed record HealthCheckBody(string Status);
}
