using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RestaurantCopilot.Application.Abstractions.Ai;
using RestaurantCopilot.Infrastructure.Ai.Gemini;

namespace RestaurantCopilot.Infrastructure.Tests.Ai;

/// <summary>Exercises the real DI registration, including the resilience pipeline.</summary>
public sealed class GeminiResilienceTests
{
    private readonly FakeHttpMessageHandler _handler = new();

    [Fact]
    public async Task GenerateAsync_RetriesRateLimitedRequestsAndSucceeds()
    {
        _handler
            .RespondWith(HttpStatusCode.TooManyRequests, "{}")
            .RespondWith(HttpStatusCode.ServiceUnavailable, "{}")
            .RespondWith(HttpStatusCode.OK, GeminiTestData.TextResponse("ok"));

        var result = await CreateLlmClient(maxRetries: 2).GenerateAsync(
            new LlmRequest(null, [new UserMessage("oi")]), TestContext.Current.CancellationToken);

        result.Value.Text.ShouldBe("ok");
        _handler.Requests.Count.ShouldBe(3);
        _handler.Requests[0].Request.Headers.GetValues("x-goog-api-key").ShouldBe(["test-key"]);
    }

    [Fact]
    public async Task GenerateAsync_WhenRetriesAreExhausted_ReturnsRateLimited()
    {
        _handler
            .RespondWith(HttpStatusCode.TooManyRequests, "{}")
            .RespondWith(HttpStatusCode.TooManyRequests, "{}");

        var result = await CreateLlmClient(maxRetries: 1).GenerateAsync(
            new LlmRequest(null, [new UserMessage("oi")]), TestContext.Current.CancellationToken);

        result.Error.ShouldBe(LlmErrors.RateLimited);
        _handler.Requests.Count.ShouldBe(2);
    }

    private ILlmClient CreateLlmClient(int maxRetries)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Gemini:ApiKey"] = "test-key",
                ["Gemini:MaxRetryAttempts"] = maxRetries.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["Gemini:RetryBaseDelay"] = "00:00:00.001",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddGemini(configuration);
        services.ConfigureHttpClientDefaults(builder => builder.ConfigurePrimaryHttpMessageHandler(() => _handler));

        return services.BuildServiceProvider().GetRequiredService<ILlmClient>();
    }
}
