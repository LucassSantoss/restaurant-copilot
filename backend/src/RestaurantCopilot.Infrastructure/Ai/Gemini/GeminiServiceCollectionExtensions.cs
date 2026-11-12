using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using RestaurantCopilot.Application.Abstractions.Ai;

namespace RestaurantCopilot.Infrastructure.Ai.Gemini;

public static class GeminiServiceCollectionExtensions
{
    public static IServiceCollection AddGemini(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<GeminiOptions>()
            .Bind(configuration.GetSection(GeminiOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<ILlmClient, GeminiClient>(ConfigureClient).AddGeminiResilience();
        services.AddHttpClient<IEmbeddingService, GeminiEmbeddingService>(ConfigureClient).AddGeminiResilience();

        // The health check gets a plain client with a short timeout: no retries, it should fail fast.
        services.AddHttpClient<GeminiHealthCheck>((provider, client) =>
        {
            ConfigureClient(provider, client);
            client.Timeout = TimeSpan.FromSeconds(5);
        });

        return services;
    }

    public static IHealthChecksBuilder AddGeminiHealthCheck(this IHealthChecksBuilder builder) =>
        // Degraded, not Unhealthy: without the LLM the AI features fail, but the API still serves data.
        builder.AddCheck<GeminiHealthCheck>("gemini", failureStatus: HealthStatus.Degraded);

    private static void ConfigureClient(IServiceProvider provider, HttpClient client)
    {
        var options = provider.GetRequiredService<IOptions<GeminiOptions>>().Value;
        client.BaseAddress = options.BaseUrl;
        if (options.IsConfigured)
        {
            client.DefaultRequestHeaders.Add("x-goog-api-key", options.ApiKey);
        }
    }

    /// <summary>
    /// Retries 429 (free-tier rate limit) and transient 5xx/timeouts with exponential backoff and
    /// jitter, honoring Retry-After. Attempt and total timeouts bound how long a request can hang.
    /// </summary>
    private static void AddGeminiResilience(this IHttpClientBuilder builder) =>
        builder.AddStandardResilienceHandler().Configure((resilience, provider) =>
        {
            var options = provider.GetRequiredService<IOptions<GeminiOptions>>().Value;

            resilience.Retry.MaxRetryAttempts = options.MaxRetryAttempts;
            resilience.Retry.Delay = options.RetryBaseDelay;
            resilience.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
            resilience.Retry.UseJitter = true;

            resilience.AttemptTimeout.Timeout = options.AttemptTimeout;
            resilience.TotalRequestTimeout.Timeout = options.TotalTimeout;

            // The circuit breaker requires a sampling window of at least twice the attempt timeout.
            resilience.CircuitBreaker.SamplingDuration = options.AttemptTimeout * 2;
        });
}
